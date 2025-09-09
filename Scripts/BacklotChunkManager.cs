#if UNITY_EDITOR
using EvroDev.BacklotUtilities.Extensions;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SLZ.Marrow.SceneStreaming;

namespace EvroDev.BacklotUtilities.Voxels
{
    public enum VisualizationMode
    {
        Gizmos,
        Backlot
    }

    public class BacklotChunkManager : MonoBehaviour
    {
        public int ChunkSize = 32;
        public List<PosToChunk> chunks = new List<PosToChunk>();

        public VisualizationMode visualizationMode = VisualizationMode.Gizmos;

        public void GenerateChangedChunks()
        {
            foreach (var chunkHolder in chunks)
            {
                if (chunkHolder.chunk.isDirty)
                {
                    chunkHolder.chunk.GenerateBacklots();
                }
            }
        }

        private void OnValidate()
        {
            if (visualizationMode == VisualizationMode.Gizmos)
            {
                foreach (var realChunk in chunks)
                {
                    var chunk = realChunk.chunk;
                    if (chunk.backlotsParent != null)
                        chunk.backlotsParent.gameObject.SetActive(false);
                    chunk.RegenGizmos();
                }
                // Hide Backlots
            }
            else
            {
                foreach (var realChunk in chunks)
                {
                    var chunk = realChunk.chunk;
                    if (chunk.backlotsParent != null)
                        chunk.backlotsParent.gameObject.SetActive(true);

                    EditorApplication.delayCall += () =>
                    {
                        foreach (SelectableFace f in chunk.GetComponentsInChildren<SelectableFace>())
                        {
                            DestroyImmediate(f.gameObject);
                        }
                    };
                }
                GenerateChangedChunks();
                // show backlots
            }
        }

        /// <summary>
        /// Check if there is a chunk at a certain chunk position
        /// </summary>
        private bool ChunkAt(Vector3Int pos, out BacklotVoxelChunk outChunk)
        {
            foreach (var chunk in chunks)
            {
                if (chunk.pos == pos)
                {
                    outChunk = chunk.chunk;
                    return true;
                }
            }
            outChunk = null;
            return false;
        }

        private Vector3Int GetChunkPos(BacklotVoxelChunk chunk)
        {
            foreach (var chunkPos in chunks)
            {
                if (chunkPos.chunk == chunk)
                {
                    return chunkPos.pos;
                }
            }
            return Vector3Int.zero;
        }

        /// <summary>
        /// Gets a voxel from relative coordinates to a specific chunk
        /// Will overflow into nearby chunks if needed
        /// </summary>
        public Voxel GetVoxel(BacklotVoxelChunk chunk, Vector3Int position)
        {
            BacklotVoxelChunk realChunk;
            Vector3Int realPos;

            if (position.InBounds(chunk.ChunkSize)) (realChunk, realPos) = (chunk, position);
            else (realChunk, realPos) = GetRelativeChunk(chunk, position);

            return realChunk.SafeSampleVoxel(realPos.x, realPos.y, realPos.z);
        }

        public Voxel GetVoxel(BacklotVoxelChunk chunk, int x, int y, int z)
        {
            return GetVoxel(chunk, new Vector3Int(x, y, z));
        }

        /// <summary>
        /// Sets a voxel from relative coordinates to a specific chunk
        /// Will overflow into nearby chunks if needed
        /// </summary>
        public void SetVoxel(BacklotVoxelChunk chunk, Vector3Int position, Voxel voxel)
        {
            BacklotVoxelChunk realChunk;
            Vector3Int realPos;

            if (position.InBounds(chunk.ChunkSize)) (realChunk, realPos) = (chunk, position);
            else (realChunk, realPos) = GetRelativeChunk(chunk, position, true);

            realChunk.SafeSetVoxel(realPos, voxel);
        }


        private (BacklotVoxelChunk, Vector3Int) GetRelativeChunk(BacklotVoxelChunk chunk, Vector3Int position, bool createIfNull = false)
        {
            Vector3Int chunkPos = GetChunkPos(chunk);
            Vector3Int originalPosition = position;

            int xChunkDist = (position.x - ((position.x >> 31) & ChunkSize)) / ChunkSize;
            int yChunkDist = (position.y - ((position.y >> 31) & ChunkSize)) / ChunkSize;
            int zChunkDist = (position.z - ((position.z >> 31) & ChunkSize)) / ChunkSize;

            chunkPos += new Vector3Int(xChunkDist, yChunkDist, zChunkDist);
            position -= new Vector3Int(xChunkDist, yChunkDist, zChunkDist) * ChunkSize;

            if (originalPosition == position)
            {
                return (chunk, originalPosition);
            }
            else if (ChunkAt(chunkPos, out var newChunk))
            {
                return (newChunk, position);
            }
            else if (createIfNull)
            {
                return (CreateNewChunk(chunkPos), position);
            }
            else
            {
                // Default to this, anything should use safesample and evaluate any voxels to empty
                return (chunk, originalPosition);
            }

        }

        private BacklotVoxelChunk CreateNewChunk(Vector3Int chunkPosition)
        {
            GameObject chunkGo = new GameObject($"Chunk ({chunkPosition.x}, {chunkPosition.y}, {chunkPosition.z})");

            chunkGo.transform.SetParent(transform);
            chunkGo.transform.localPosition = chunkPosition * ChunkSize;

            BacklotVoxelChunk chunk = chunkGo.AddComponent<BacklotVoxelChunk>();

            chunk.manager = this;
            chunk.ChunkSize = ChunkSize;

            chunks.Add(new PosToChunk()
            {
                pos = chunkPosition,
                chunk = chunk
            });

            chunk.Regen();

            return chunk;
        }

        public void ShiftSelect(SelectableFace newFace)
        {
            foreach (GameObject gobj in Selection.gameObjects)
            {
                if (gobj.TryGetComponent(out SelectableFace face1) && face1 != newFace)
                {
                    SelectRectBetweenTwo(face1, newFace);
                }
            }
        }

        private void SelectRectBetweenTwo(SelectableFace face1, SelectableFace face2)
        {
            BacklotVoxelChunk chunk1 = face1.chunk;
            BacklotVoxelChunk chunk2 = face2.chunk;

            Vector3Int chunkOffset = Vector3Int.zero;

            if (chunk1 != chunk2)
            {
                chunkOffset = (GetChunkPos(chunk2) - GetChunkPos(chunk1)) * ChunkSize;
            }

            chunkOffset += face2.voxelPosition - face1.voxelPosition;

            Vector3Int chunkDirection = chunkOffset;
            chunkDirection.Clamp(Vector3Int.one * -1, Vector3Int.one);
            if (chunkDirection.x == 0) chunkDirection.x = 1;
            if (chunkDirection.y == 0) chunkDirection.y = 1;
            if (chunkDirection.z == 0) chunkDirection.z = 1;

            List<ManagerFaceSelection> output = new();

            for (int x = 0; Mathf.Abs(x) <= Mathf.Abs(chunkOffset.x); x += chunkDirection.x)
            {
                for (int y = 0; Mathf.Abs(y) <= Mathf.Abs(chunkOffset.y); y += chunkDirection.y)
                {
                    for (int z = 0; Mathf.Abs(z) <= Mathf.Abs(chunkOffset.z); z += chunkDirection.z)
                    {
                        // int complexDialogResult = EditorUtility.DisplayDialogComplex("Dialog", $"Local position from Start: ({x}, {y}, {z})", "Next", "Cancel", "Skip");
                        // if (complexDialogResult == 1)
                        // {
                        //     return;
                        // }
                        var offset = face1.voxelPosition + new Vector3Int(x, y, z);
                        var cuhChunk = GetRelativeChunk(chunk1, offset);

                        var testVoxel = GetVoxel(cuhChunk.Item1, cuhChunk.Item2);
                        if (testVoxel.IsEmpty)
                        {
                            continue;
                        }

                        output.Add(new ManagerFaceSelection(cuhChunk.Item1, cuhChunk.Item2, face1.FaceDirection));
                    }
                }
            }

            List<GameObject> newFounds = new List<GameObject>();
            foreach (ManagerFaceSelection selection in output)
            {
                var matching = GetComponentsInChildren<SelectableFace>().Where(p => p.chunk == selection.chunk && p.voxelPosition == selection.localPosition && p.FaceDirection == selection.direciton).ToArray();
                if (matching.Length > 0)
                {
                    newFounds.Add(matching[0].gameObject);
                }
            }
            if (newFounds.Count != 0)
            {
                EditorApplication.delayCall += () =>
                {
                    Selection.objects = Selection.gameObjects.Concat(newFounds).Distinct().ToArray();
                };
            }
        }

        public void FloodFillSelect(List<SelectableFace> startingFaces, bool discriminateType = false)
        {
            List<GameObject> newFounds = new List<GameObject>();
            foreach (SelectableFace face in startingFaces)
            {
                foreach (ManagerFaceSelection selection in FloodFillFaces(face, discriminateType))
                {
                    var matching = GetComponentsInChildren<SelectableFace>().Where(p => p.chunk == selection.chunk && p.voxelPosition == selection.localPosition && p.FaceDirection == selection.direciton).ToArray();
                    if (matching.Length > 0)
                    {
                        newFounds.Add(matching[0].gameObject);
                    }
                }
            }
            if (newFounds.Count != 0)
            {
                Selection.objects = Selection.gameObjects.Concat(newFounds).Distinct().ToArray();
            }
        }

        public ManagerFaceSelection[] FloodFillFaces(SelectableFace startingFace, bool discriminateType = false)
        {
            List<ManagerFaceSelection> outputFaces = new();

            var targetDirection = startingFace.FaceDirection;
            var relativeChunk = startingFace.chunk;

            HashSet<Vector3Int> visitedVoxels = new HashSet<Vector3Int>(); // Do Everything in relation to the starting chunk. Ugh
            Queue<Vector3Int> Q = new Queue<Vector3Int>();

            Q.Enqueue(startingFace.voxelPosition);
            Voxel targetVoxel = startingFace.chunk.SafeSampleVoxel(startingFace.voxelPosition.x, startingFace.voxelPosition.y, startingFace.voxelPosition.z);
            visitedVoxels.Add(startingFace.voxelPosition);

            while(Q.Count > 0)
            {
                Vector3Int n = Q.Dequeue();
                var cuhChunk = GetRelativeChunk(relativeChunk, n);
                outputFaces.Add(new ManagerFaceSelection(cuhChunk.Item1, cuhChunk.Item2, targetDirection));

                Vector3Int[] directions = targetDirection switch
                {
                    FaceDirection.Forward => new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right },
                    FaceDirection.Backward => new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right },
                    FaceDirection.Up => new Vector3Int[] { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right },
                    FaceDirection.Down => new Vector3Int[] { Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right },
                    FaceDirection.Left => new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back },
                    FaceDirection.Right => new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.forward, Vector3Int.back },
                    _ => new Vector3Int[0]
                };

                Vector3Int scanDirection = targetDirection switch
                {
                    FaceDirection.Forward => Vector3Int.forward,
                    FaceDirection.Backward => Vector3Int.back,
                    FaceDirection.Up => Vector3Int.up,
                    FaceDirection.Down => Vector3Int.down,
                    FaceDirection.Left => Vector3Int.left,
                    FaceDirection.Right => Vector3Int.right,
                    _ => Vector3Int.forward
                };

                foreach(Vector3Int direction in directions)
                {
                    Vector3Int neighborPos = n + direction;
                    Vector3Int faceNeighbor = neighborPos + scanDirection;
                    Voxel current = GetVoxel(relativeChunk, neighborPos.x, neighborPos.y, neighborPos.z);

                    if(current.IsEmpty) continue;
                    if(visitedVoxels.Contains(neighborPos)) continue; 

                    if(discriminateType)
                    {
                        if(targetVoxel.GetMaterial(targetDirection) != current.GetMaterial(targetDirection)) continue;
                        string barcode1 = targetVoxel.GetSurface(targetDirection).Barcode.ID;
                        string barcode2 = current.GetSurface(targetDirection).Barcode.ID;
                        if(barcode1 != barcode2) continue;
                        if(targetVoxel.GetOverrideFace(targetDirection) != current.GetOverrideFace(targetDirection)) continue;
                    }

                    Voxel inTheFace = GetVoxel(relativeChunk, faceNeighbor.x, faceNeighbor.y, faceNeighbor.z);
                    if(inTheFace.IsEmpty)
                    {
                        Q.Enqueue(neighborPos);
                        visitedVoxels.Add(neighborPos);
                    }
                }
            }

            return outputFaces.ToArray();
        }

        void Reset()
        {
            var chunk = CreateNewChunk(Vector3Int.zero);
            SetVoxel(chunk, Vector3Int.zero, new Voxel()
            {
                IsEmpty = false
            });
        }
    }

    [Serializable]
    public struct PosToChunk
    {
        public Vector3Int pos;
        public BacklotVoxelChunk chunk;
    }

    public struct ManagerFaceSelection
    {
        public BacklotVoxelChunk chunk;
        public Vector3Int localPosition;
        public FaceDirection direciton;

        public ManagerFaceSelection(BacklotVoxelChunk chunk, Vector3Int localPos, FaceDirection dir)
        {
            this.chunk = chunk;
            localPosition = localPos;
            direciton = dir;
        }
    }
}
#endif