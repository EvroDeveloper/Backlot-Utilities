#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using EvroDev.BacklotUtilities.Extensions;
using SLZ.Marrow.Warehouse;
using SLZ.Marrow;




#if UNITY_EDITOR
using UnityEditor;
#endif


namespace EvroDev.BacklotUtilities.Voxels
{

    public struct SurfaceDescription
    {
        public Material material;
        public string surfaceDataCard;
        public int id;
    }

    public class BacklotVoxelChunk : MonoBehaviour
    {
        public BacklotChunkManager manager;
        public int ChunkSize = 32;
        int ChunkSizeP => ChunkSize + 2;
        int ChunkSizeP2 => ChunkSizeP * ChunkSizeP;
        
        [HideInInspector]
        public Voxel[] voxels;

        public Transform tempGizmosParent;
        public Transform backlotsParent;
        private List<SelectableFace> gizmoFaces = new List<SelectableFace>();

        [HideInInspector]
        public bool isDirty = false;

        public Voxel GetVoxel(Vector3Int position)
        {
            if (position.InBounds(ChunkSize)) return SafeSampleVoxel(position.x, position.y, position.z);
            return manager.GetVoxel(this, position);
        }

        public Voxel GetVoxel(int x, int y, int z)
        {
            return GetVoxel(new Vector3Int(x, y, z));
        }


        public Voxel SafeSampleVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= ChunkSize)
            {
                return new Voxel()
                {
                    IsEmpty = true
                };
            }
            if (y < 0 || y >= ChunkSize)
            {
                return new Voxel()
                {
                    IsEmpty = true
                };
            }
            if (z < 0 || z >= ChunkSize)
            {
                return new Voxel()
                {
                    IsEmpty = true
                };
            }

            int index = x + ChunkSize * (y + ChunkSize * z);
            return voxels[index];
        }


        public void SetVoxel(int x, int y, int z, Voxel voxel)
        {
            manager.SetVoxel(this, new Vector3Int(x, y, z), voxel);
        }

        public void SafeSetVoxel(Vector3Int position, Voxel voxel)
        {
            if (position.x < 0 || position.x >= ChunkSize)
            {
                return;
            }
            if (position.y < 0 || position.y >= ChunkSize)
            {
                return;
            }
            if (position.z < 0 || position.z >= ChunkSize)
            {
                return;
            }
            int index = position.x + ChunkSize * (position.y + ChunkSize * position.z);
            voxels[index] = voxel;
            isDirty = true;
            RegenGizmo();
        }


        [ContextMenu("Regen Full")]
        public void Regen()
        {
            voxels = new Voxel[ChunkSize * ChunkSize * ChunkSize];
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        int index = x + ChunkSize * (y + ChunkSize * z);
                        voxels[index] = new Voxel()
                        {
                            IsEmpty = true
                        };
                        isDirty = true;
                    }
                }
            }
            RegenGizmo();
        }

        [ContextMenu("Regen Gizmos")]
        public void RegenGizmos()
        {
            EditorApplication.delayCall += () => RegenGizmo(new List<Vector3Int>(), true);
        }

        void RegenGizmo(List<Vector3Int> onlyRegen, bool force = false)
        {
            if (!isDirty && !force) return;

            foreach (SelectableFace f in GetComponentsInChildren<SelectableFace>())
            {
                if (onlyRegen.Count == 0 || onlyRegen.Contains(f.voxelPosition))
                    DestroyImmediate(f.gameObject);
            }

            if (tempGizmosParent == null)
            {
                GameObject gizmoParent = new GameObject("TEMP Gizmo Parent");
                gizmoParent.transform.parent = transform;
                gizmoParent.transform.localPosition = Vector3.zero;
                tempGizmosParent = gizmoParent.transform;
            }

            tempGizmosParent.gameObject.hideFlags = HideFlags.HideInHierarchy;

            if (onlyRegen.Count != 0)
            {
                foreach(Vector3Int pos in onlyRegen)
                {
                    GenerateGizmoFaces(pos.x, pos.y, pos.z);
                }
            }
            else
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        for (int z = 0; z < ChunkSize; z++)
                        {
                            GenerateGizmoFaces(x, y, z);
                        }
                    }
                }
            }

            void GenerateGizmoFaces(int x, int y, int z)
            {
                Voxel thisVoxel = SafeSampleVoxel(x, y, z);
                if (thisVoxel == null || thisVoxel.IsEmpty) return;

                if (thisVoxel.type == VoxelType.Wall)
                {
                    if (GetVoxel(x, y + 1, z).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Up, thisVoxel, this);
                    }
                    if (GetVoxel(x, y - 1, z).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Down, thisVoxel, this);
                    }
                    if (GetVoxel(x, y, z + 1).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Forward, thisVoxel, this);
                    }
                    if (GetVoxel(x, y, z - 1).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Backward, thisVoxel, this);
                    }
                    if (GetVoxel(x + 1, y, z).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Right, thisVoxel, this);
                    }
                    if (GetVoxel(x - 1, y, z).IsEmpty)
                    {
                        SelectableFace.Create(tempGizmosParent, x, y, z, FaceDirection.Left, thisVoxel, this);
                    }
                }
                else if (thisVoxel.type == VoxelType.CornerConcave1m)
                {
                    // todo gizmo
                }
            }
        }

        void RegenGizmo()
        {
            RegenGizmo(new List<Vector3Int>());
        }

        public struct VoxelFaceSelection
        {
            public Vector3Int position;
            public FaceDirection direction;

            public VoxelFaceSelection(Vector3Int pos, FaceDirection dir)
            {
                position = pos;
                direction = dir;
            }

            public static List<VoxelFaceSelection> FromSelection()
            {
                List<VoxelFaceSelection> outpt = new List<VoxelFaceSelection>();
                foreach (GameObject g in Selection.gameObjects)
                {
                    if (g.TryGetComponent(out SelectableFace face))
                    {
                        outpt.Add(new VoxelFaceSelection(face.voxelPosition, face.FaceDirection));
                    }
                }
                return outpt;
            }
        }

        void RegenGizmo(List<VoxelFaceSelection> newSelection)
        {
            RegenGizmo(newSelection, new List<Vector3Int>());
        }

        void RegenGizmo(List<VoxelFaceSelection> newSelection, List<Vector3Int> onlyRegen)
        {
            RegenGizmo(onlyRegen);
            List<GameObject> newFounds = new List<GameObject>();
            foreach (VoxelFaceSelection selection in newSelection)
            {
                var matching = GetComponentsInChildren<SelectableFace>().Where(p => p.voxelPosition == selection.position && p.FaceDirection == selection.direction).ToArray();
                if (matching.Length > 0)
                {
                    newFounds.Add(matching[0].gameObject);
                }
            }
            if (newFounds.Count != 0)
            {
                Selection.objects = Selection.gameObjects.Concat(newFounds).Distinct().ToArray();
            }
        }


        [ContextMenu("Set Material to Selected")]
        void SetWithSelectedMaterial()
        {
#if UNITY_EDITOR
            foreach (Object g in Selection.objects)
            {
                if (g is Material)
                {
                    PaintSelection(g as Material);
                    break;
                }
            }
#endif
        }

        [ContextMenu("Generate Backlot")]
        public void GenerateBacklots()
        {
            ulong[,,] axis_cols = new ulong[3, ChunkSizeP, ChunkSizeP];
            ulong[,,] col_face_masks = new ulong[6, ChunkSizeP, ChunkSizeP];

            for (int y = -1; y < ChunkSize + 1; y++)
            {
                for (int z = -1; z < ChunkSize + 1; z++)
                {
                    for (int x = -1; x < ChunkSize + 1; x++)
                    {
                        Vector3Int pos = new(x, y, z);
                        Voxel b = GetVoxel(x, y, z);
                        if (!b.IsEmpty)
                        {
                            axis_cols[0, z + 1, x + 1] |= 1ul << (y + 1);
                            axis_cols[1, y + 1, z + 1] |= 1ul << (x + 1);
                            axis_cols[2, y + 1, x + 1] |= 1ul << (z + 1);
                        }
                    }
                }
            }

            // For every main axis
            for (int axis = 0; axis < 3; axis++)
            {
                for (int x = 0; x < ChunkSizeP; x++)
                {
                    for (int z = 0; z < ChunkSizeP; z++)
                    {
                        ulong col = axis_cols[axis, z, x];
                        col_face_masks[2 * axis + 0, z, x] = col & ~(col << 1);
                        col_face_masks[2 * axis + 1, z, x] = col & ~(col >> 1);
                    }
                }
            }

            Dictionary<VoxelType, Dictionary<SurfaceDescription, Dictionary<int, uint[]>>>[] data = new Dictionary<VoxelType, Dictionary<SurfaceDescription, Dictionary<int, uint[]>>>[6]
            {
                new(),
                new(),
                new(),
                new(),
                new(),
                new()
            };

            // For each of the 6 directions
            for (byte axis2 = 0; axis2 < 6; axis2++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        int col_index = 1 + x + ((z + 1) * ChunkSizeP) + ChunkSizeP2 * axis2;

                        ulong col = col_face_masks[axis2, z + 1, x + 1];
                        col >>= 1;
                        col &= ~(1ul << ChunkSize);

                        while (col != 0)
                        {
                            int y = GreedyGridwall.CountTrailingZeros(col);

                            col &= col - 1;

                            Vector3Int voxelPos = new(x, z, y);

                            switch (axis2)
                            {
                                case (0):
                                    voxelPos = new Vector3Int(x, y, z);
                                    break;
                                case (1):
                                    voxelPos = new Vector3Int(x, y, z);
                                    break;
                                case (2):
                                    voxelPos = new Vector3Int(y, z, x);
                                    break;
                                case (3):
                                    voxelPos = new Vector3Int(y, z, x);
                                    break;
                            }

                            Voxel currentVoxel = SafeSampleVoxel(voxelPos.x, voxelPos.y, voxelPos.z);

                            byte facingDirection = 0;

                            facingDirection = axis2 switch
                            {
                                (4) => 5,
                                (5) => 4,
                                _ => axis2,
                            };
                            if (currentVoxel.GetOverrideFace((FaceDirection)facingDirection)) continue;

                            Material faceMat = currentVoxel.GetMaterial((FaceDirection)facingDirection);
                            VoxelType type = currentVoxel.type;

                            if(faceMat == null)
                            {
                                faceMat = BacklotManager.DefaultGridMaterial();
                            }

                            SurfaceDescription surfaceDescription = new SurfaceDescription()
                            {
                                material = faceMat,
                                surfaceDataCard = currentVoxel.GetSurface((FaceDirection)facingDirection).Barcode.ID,
                                id = currentVoxel.GetDiscriminator((FaceDirection)facingDirection)
                            };

                            // WHAT THE FUCK :fireEmoji:
                            if (!data[axis2].ContainsKey(type))
                            {
                                data[axis2].Add(type, new Dictionary<SurfaceDescription, Dictionary<int, uint[]>>());
                            }
                            if (!data[axis2][type].ContainsKey(surfaceDescription))
                            {
                                data[axis2][type].Add(surfaceDescription, new Dictionary<int, uint[]>());
                            }
                            if (!data[axis2][type][surfaceDescription].ContainsKey(y))
                            {
                                data[axis2][type][surfaceDescription].Add(y, new uint[ChunkSize]);
                            }
                            data[axis2][type][surfaceDescription][y][x] |= 1u << z;
                        }
                    }
                }
            }

            List<GreedyGridwall.ResultingBacklot> backlotGenResults = new List<GreedyGridwall.ResultingBacklot>();

            // add a list of Backlot Faces rn for here

            for (int axis = 0; axis < data.Length; axis++)
            {
                foreach(var type in data[axis].Keys)
                {
                    if(type != VoxelType.Wall)
                        continue;
                    
                    Dictionary<SurfaceDescription, Dictionary<int, uint[]>> block_mat_data = data[axis][type];

                    foreach (var surfaceDescription in block_mat_data.Keys)
                    {
                        var axis_plane = block_mat_data[surfaceDescription];

                        foreach (var axisPos in axis_plane.Keys)
                        {
                            uint[] plane = axis_plane[axisPos];

                            // now FINALLY greedymesh it
                            Debug.Log($"Axis: {axis_plane}\nAxis Pos: {axisPos}\n\"Axis plane int: {plane[0]}");
                            var generatedGrids = GreedyGridwall.GreedTheGrid(plane, surfaceDescription, (FaceDirection)axis, axisPos);
                            backlotGenResults.AddRange(generatedGrids);
                        }
                    }
                }
            }

            if (backlotsParent != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Defer the destruction to avoid OnValidate timing issues
                    EditorApplication.delayCall += () =>
                    {
                        DestroyImmediate(backlotsParent.gameObject);

                        GenerateBacklotPrefabs(backlotGenResults);
                    };
                }
#endif
            }
            else
            {
                GenerateBacklotPrefabs(backlotGenResults);
            }
        }

        private void GenerateBacklotPrefabs(List<GreedyGridwall.ResultingBacklot> backlotGenResults) 
        {
            GameObject backParent = new GameObject("Generated Backlots");
            backParent.transform.parent = transform;
            backParent.transform.localPosition = Vector3.zero;
            backlotsParent = backParent.transform;

            foreach (var backlot in backlotGenResults)
            {
                var gridwall = BacklotManager.FindGridWall(backlot.scale);
                if (gridwall != null)
                {
                    GameObject inst = PrefabUtility.InstantiatePrefab(gridwall, backlotsParent) as GameObject;
                    inst.GetComponentInChildren<MeshRenderer>().sharedMaterial = backlot.material;
                    inst.GetComponentInChildren<ImpactProperties>().SurfaceDataCard = new DataCardReference<SurfaceDataCard>(backlot.surfaceDataBarcode);
                    inst.transform.localPosition = backlot.localPos;
                    inst.transform.rotation = backlot.rotation;
                }
            }
            isDirty = false;
        }

        [ContextMenu("Extrude Selection (Debug)")]
        public void ExtrudeSelection()
        {
#if UNITY_EDITOR
            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();
            foreach (GameObject g in Selection.gameObjects)
            {
                if (g.TryGetComponent(out SelectableFace face))
                {
                    if (face.chunk != this) continue;
                    Internal_ExtrudeFace(face, newSelection);
                }
            }
            isDirty = true;
            RegenGizmo(newSelection);
#endif
        }

        public void ExtrudeFaceGizmo(SelectableFace face, int length = 1)
        {
            if (face.chunk != this) return;

            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();

            List<Vector3Int> gizmosToUpdate = new List<Vector3Int>() {
                face.GetTargetAir(),
                face.GetTargetAir() + Vector3Int.forward,
                face.GetTargetAir() - Vector3Int.forward,
                face.GetTargetAir() + Vector3Int.right,
                face.GetTargetAir() - Vector3Int.right,
                face.GetTargetAir() + Vector3Int.up,
                face.GetTargetAir() + Vector3Int.down
            };

            Internal_ExtrudeFace(face, newSelection);

            isDirty = true;
            RegenGizmo(newSelection, gizmosToUpdate);
        }

        public void ExtrudeFaceGizmos(List<SelectableFace> faces)
        {
            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();
            HashSet<Vector3Int> gizmosToUpdate = new HashSet<Vector3Int>();
            for (int i = 0; i < faces.Count; i++)
            {
                SelectableFace face = faces[i];
                EditorUtility.DisplayProgressBar("Extruding", $"Extruding face {i} of {faces.Count}", (float)i / faces.Count);
                if (face.chunk != this) continue;

                Internal_ExtrudeFace(face, newSelection);

                gizmosToUpdate.Add(face.GetTargetAir());
                gizmosToUpdate.Add(face.GetTargetAir() + Vector3Int.forward);
                gizmosToUpdate.Add(face.GetTargetAir() - Vector3Int.forward);
                gizmosToUpdate.Add(face.GetTargetAir() + Vector3Int.right);
                gizmosToUpdate.Add(face.GetTargetAir() - Vector3Int.right);
                gizmosToUpdate.Add(face.GetTargetAir() + Vector3Int.up);
                gizmosToUpdate.Add(face.GetTargetAir() + Vector3Int.down);
            }
            EditorUtility.ClearProgressBar();

            isDirty = true;
            RegenGizmo(newSelection, gizmosToUpdate.ToList());
        }

        void Internal_ExtrudeFace(SelectableFace face, List<VoxelFaceSelection> faceSelectionToAppend)
        {
            Vector3Int newFace = face.GetTargetAir();

            Voxel targetVoxel = GetVoxel(newFace.x, newFace.y, newFace.z);

            if (!newFace.InBounds(ChunkSize))
            {
                Voxel newVox = new Voxel()
                {
                    IsEmpty = false,
                    type = VoxelType.Wall,
                };
                newVox.SetMaterial(face.FaceDirection, face.material);
                SetVoxel(newFace.x, newFace.y, newFace.z, newVox);
            }
            else
            {
                targetVoxel.SetMaterial(face.FaceDirection, face.material);
                targetVoxel.IsEmpty = false;
            }

            faceSelectionToAppend.Add(new VoxelFaceSelection(newFace, face.FaceDirection));
        }

        [ContextMenu("Intrude Selection (Debug)")]
        public void IntrudeSelection()
        {
#if UNITY_EDITOR
            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();
            foreach (GameObject g in Selection.gameObjects)
            {
                if (g.TryGetComponent(out SelectableFace face))
                {
                    if (face.chunk != this) continue;
                    Internal_IntrudeFace(face, newSelection);
                }
            }
            isDirty = true;
            RegenGizmo(newSelection);
#endif
        }

        public void IntrudeFaceGizmo(SelectableFace face)
        {
            if (face.chunk != this) return;

            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();

            List<Vector3Int> gizmosToUpdate = new List<Vector3Int>() {
                face.voxelPosition,
                face.voxelPosition + Vector3Int.forward,
                face.voxelPosition - Vector3Int.forward,
                face.voxelPosition + Vector3Int.right,
                face.voxelPosition - Vector3Int.right,
                face.voxelPosition + Vector3Int.up,
                face.voxelPosition + Vector3Int.down
            };

            Internal_IntrudeFace(face, newSelection);

            isDirty = true;
            RegenGizmo(newSelection, gizmosToUpdate);
        }

        public void IntrudeFaceGizmos(List<SelectableFace> faces)
        {
            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();
            HashSet<Vector3Int> gizmosToUpdate = new HashSet<Vector3Int>();

            foreach (SelectableFace face in faces)
            {
                if (face.chunk != this) continue;

                Internal_IntrudeFace(face, newSelection);

                gizmosToUpdate.Add(face.voxelPosition);
                gizmosToUpdate.Add(face.voxelPosition + Vector3Int.forward);
                gizmosToUpdate.Add(face.voxelPosition - Vector3Int.forward);
                gizmosToUpdate.Add(face.voxelPosition + Vector3Int.right);
                gizmosToUpdate.Add(face.voxelPosition - Vector3Int.right);
                gizmosToUpdate.Add(face.voxelPosition + Vector3Int.up);
                gizmosToUpdate.Add(face.voxelPosition + Vector3Int.down);
            }

            isDirty = true;
            EditorUtility.SetDirty(this);
            RegenGizmo(newSelection, gizmosToUpdate.ToList());
        }

        private void Internal_IntrudeFace(SelectableFace face, List<VoxelFaceSelection> faceSelectionToAppend)
        {
            Vector3Int newFace = face.GetBackwardPos();

            SafeSampleVoxel(newFace.x, newFace.y, newFace.z).SetMaterial(face.FaceDirection, face.material);
            Vector3Int p = face.voxelPosition;
            SafeSampleVoxel(p.x, p.y, p.z).IsEmpty = true;

            faceSelectionToAppend.Add(new VoxelFaceSelection(newFace, face.FaceDirection));
        }

        public void PaintSelection(Material m)
        {
#if UNITY_EDITOR
            List<VoxelFaceSelection> newSelection = new List<VoxelFaceSelection>();
            foreach (GameObject g in Selection.gameObjects)
            {
                if (g.TryGetComponent(out SelectableFace face))
                {
                    if (face.chunk != this) continue;

                    Vector3Int p = face.voxelPosition;
                    Voxel voxel = SafeSampleVoxel(p.x, p.y, p.z);
                    voxel.SetMaterial(face.FaceDirection, m);
                    newSelection.Add(new VoxelFaceSelection(p, face.FaceDirection));
                }
            }
            isDirty = true;
            RegenGizmo(newSelection);
#endif
        }

        public void FloodFillSelect(List<SelectableFace> startingFaces, bool discriminateType = false)
        {
            List<GameObject> newFounds = new List<GameObject>();
            foreach(SelectableFace face in startingFaces)
            {
                foreach (VoxelFaceSelection selection in FloodFillVoxels(face.voxelPosition, face.FaceDirection, discriminateType))
                {
                    var matching = GetComponentsInChildren<SelectableFace>().Where(p => p.voxelPosition == selection.position && p.FaceDirection == selection.direction).ToArray();
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

        List<VoxelFaceSelection> FloodFillVoxels(Vector3Int startingFace, FaceDirection targetDirection, bool discriminateType = false)
        {
            List<VoxelFaceSelection> outputFaces = new List<VoxelFaceSelection>();
            HashSet<Vector3Int> visitedVoxels = new HashSet<Vector3Int>();
            Queue<Vector3Int> Q = new Queue<Vector3Int>();

            Q.Enqueue(startingFace);
            Voxel targetVoxel = SafeSampleVoxel(startingFace.x, startingFace.y, startingFace.z);
            visitedVoxels.Add(startingFace);

            while(Q.Count > 0)
            {
                var n = Q.Dequeue();
                outputFaces.Add(new VoxelFaceSelection(n, targetDirection));

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
                    Voxel current = SafeSampleVoxel(neighborPos.x, neighborPos.y, neighborPos.z);

                    if(current.IsEmpty) continue;
                    if(visitedVoxels.Contains(neighborPos)) continue; 

                    if(discriminateType)
                    {
                        if(targetVoxel.GetMaterial(targetDirection) != current.GetMaterial(targetDirection)) continue;
                        Debug.Log(current.GetSurface(targetDirection));
                        string barcode1 = targetVoxel.GetSurface(targetDirection).Barcode.ID;
                        string barcode2 = current.GetSurface(targetDirection).Barcode.ID;
                        if(barcode1 != barcode2) continue;
                        Debug.Log("R");
                        if(targetVoxel.GetOverrideFace(targetDirection) != current.GetOverrideFace(targetDirection)) continue;
                    }

                    Voxel inTheFace = SafeSampleVoxel(faceNeighbor.x, faceNeighbor.y, faceNeighbor.z);
                    if(inTheFace.IsEmpty)
                    {
                        Q.Enqueue(neighborPos);
                        visitedVoxels.Add(neighborPos);
                    }
                }
            }
            return outputFaces;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Vector3 size = new Vector3(ChunkSize, ChunkSize, ChunkSize);
            Gizmos.DrawWireCube(transform.position + (size / 2), size);
        }
    }

    [System.Flags]
    public enum VoxelType
    {
        Wall = 1,
        CornerConcave1m = 2,
        CornerConcave2m = 4,
    }

    [System.Serializable]
    public class Voxel
    {
        public bool IsEmpty = false;
        public VoxelType type = VoxelType.Wall;

        [SerializeField]
        private Material[] _materials = new Material[6];
        [SerializeField]
        private byte _overrideFacesByte = new();
        private DataCardReference<SurfaceDataCard>[] faceSurfaces = new DataCardReference<SurfaceDataCard>[6];
        private int[] discriminators = new int[6];
        //private bool[] _overrideFaces = new bool[6];

        public void SetOverrideFace(FaceDirection dir, bool enabled)
        {
            if (enabled) 
            {
                _overrideFacesByte = (byte)(_overrideFacesByte | (1 << (int)dir));
            }
            else 
            {
                _overrideFacesByte = (byte)(_overrideFacesByte & ~(1 << (int)dir));
            }

            //_overrideFaces[(int)dir] = enabled;
        }

        public bool GetOverrideFace(FaceDirection dir)
        {
            return (_overrideFacesByte & (1 << (int)dir)) != 0;

            //return _overrideFaces[(int)dir];
        }


        public void SetMaterial(FaceDirection dir, Material mat)
        {
            _materials[(int)dir] = mat;
        }

        public Material GetMaterial(FaceDirection dir)
        {
            return _materials[(int)dir];
        }

        public void SetSurface(FaceDirection dir, DataCardReference<SurfaceDataCard> surface)
        {
            faceSurfaces[(int)dir] = surface;
        }
        public DataCardReference<SurfaceDataCard> GetSurface(FaceDirection dir)
        {
            return faceSurfaces[(int)dir];
        }

        public void SetDiscriminator(FaceDirection dir, int disc)
        {
            discriminators[(int)dir] = disc;
        }

        public int GetDiscriminator(FaceDirection dir)
        {
            return discriminators[(int)dir];
        }
    }


    public class GreedyGridwall
    {
        public class ResultingBacklot
        {
            public Vector2 planePos;
            public Vector3 localPos;
            public Vector2 scale;
            public FaceDirection axis;
            public Barcode surfaceDataBarcode;
            public int axisIndex;
            public Quaternion rotation;
            public Material material;
        }

        public static int CountTrailingZeros(ulong n)
        {
            if (n == 0) return 64; // Assuming 32-bit integer
            int count = 0;
            while ((n & 1) == 0)
            {
                n >>= 1;
                count++;
            }
            return count;
        }
        public static int CountTrailingZeros(int n)
        {
            if (n == 0) return 32; // Assuming 32-bit integer
            int count = 0;
            while ((n & 1) == 0)
            {
                n >>= 1;
                count++;
            }
            return count;
        }

        public static List<int> ValidLengths = new List<int>
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            8,
            10,
            12,
            14,
            16,
            18,
            20
        };

        public static int GetValidBacklotLength(int realLength)
        {
            if (ValidLengths.Contains(realLength)) return realLength;

            for (int i = 1; i < ValidLengths.Count; i++)
            {
                if (ValidLengths[i - 1] < realLength && ValidLengths[i] > realLength)
                {
                    return ValidLengths[i - 1];
                }
            }
            return ValidLengths[ValidLengths.Count - 1];
        }

        public static List<ResultingBacklot> GreedTheGrid(uint[] data, SurfaceDescription surfDesc, FaceDirection axis, int axisPos)
        {
            List<ResultingBacklot> outpt = new List<ResultingBacklot>();
            for (int x = 0; x < data.Length; x++)
            {
                int y = 0;
                while (y < 32)
                {
                    y += CountTrailingZeros(data[x] >> y);
                    int height = GetValidBacklotLength(CountTrailingZeros(~(data[x] >> y)));

                    if (height == 0) break;

                    uint h_as_mask = (1u << height) - 1;
                    uint mask = h_as_mask << y;
                    int w = 1;


                    while (x + w < data.Length)
                    {
                        long nextRow = (data[x + w] >> y) & h_as_mask;
                        bool brokenInBigJump = false;

                        if (nextRow != h_as_mask)
                            break;

                        // if the next one is NOT a valid
                        // 6, 8
                        // If w = 6 (cant do 7 because not valid)
                        if (!ValidLengths.Contains(w + 1))
                        {
                            int validW = w; // w = 6
                                            // If it can, &= ~mask them all
                                            // If it cant, break (out of all ugh)
                            int nextValidIndex = ValidLengths.IndexOf(w) + 1;
                            if (nextValidIndex >= ValidLengths.Count)
                            {
                                brokenInBigJump = true;
                                break;
                            }

                            int nextValidSize = ValidLengths[nextValidIndex];
                            // See if it can extend ALL THE WAY to the next valid one
                            // Will break out after checking extraW as 2, bc 8 is contained
                            for (int extraW = 1; !ValidLengths.Contains(w + extraW); extraW++)
                            {
                                // checks at 7
                                // checks at 8

                                if (x + w + extraW >= data.Length)
                                {
                                    w = validW;
                                    brokenInBigJump = true;
                                    break;
                                }

                                long nextNextRow = (data[x + w + extraW] >> y) & h_as_mask;
                                if (nextNextRow != h_as_mask || x + w + extraW > data.Length - 1)
                                {
                                    w = validW;
                                    brokenInBigJump = true;
                                    break;
                                }
                                else if (ValidLengths.Contains(w + extraW))
                                {
                                    // Should happen when evaluating at 8, and none of the previous were broken
                                    // Means it can extend all the way to the next valid length. Set w to the new value and &= ~ the stuffs

                                    for (int g = 0; g < extraW; g++)
                                    {
                                        data[x + w + g] &= ~mask;
                                    }
                                    w += extraW - 1;
                                    break;
                                }
                                // im actually dying this code hurts me
                            }
                        }

                        if (brokenInBigJump) break;

                        data[x + w] &= ~mask;
                        w += 1;
                    }

                    Vector2 scale = new Vector2(w, height);

                    var backlot = new ResultingBacklot()
                    {
                        planePos = new Vector2(x, y) + (scale / 2),
                        scale = scale,
                        material = surfDesc.material,
                        //surfaceDataBarcode = new Barcode("SLZ.Backlot.SurfaceDataCard.Concrete"),
                        surfaceDataBarcode = surfDesc.surfaceDataCard != null ? new Barcode(surfDesc.surfaceDataCard) : new Barcode("SLZ.Backlot.SurfaceDataCard.Concrete"),
                        axis = axis,
                        axisIndex = axisPos
                    }; 

                    bool normal = scale.y >= scale.x;

                    switch (axis)
                    {
                        case FaceDirection.Forward:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.up : Vector3.right, Vector3.forward);
                            backlot.localPos = new Vector3(backlot.planePos.x, backlot.planePos.y, axisPos);
                            break;
                        case FaceDirection.Backward:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.up : Vector3.right, Vector3.back);
                            backlot.localPos = new Vector3(backlot.planePos.x, backlot.planePos.y, axisPos + 1);
                            break;
                        case FaceDirection.Up:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.forward : Vector3.right, Vector3.down);
                            backlot.localPos = new Vector3(backlot.planePos.x, axisPos + 1, backlot.planePos.y);
                            break;
                        case FaceDirection.Down:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.forward : Vector3.right, Vector3.up);
                            backlot.localPos = new Vector3(backlot.planePos.x, axisPos, backlot.planePos.y);
                            break;
                        case FaceDirection.Left:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.up : Vector3.forward, Vector3.right);
                            backlot.localPos = new Vector3(axisPos, backlot.planePos.y, backlot.planePos.x);
                            break;
                        case FaceDirection.Right:
                            backlot.rotation = Quaternion.LookRotation(normal ? Vector3.up : Vector3.forward, Vector3.left);
                            backlot.localPos = new Vector3(axisPos + 1, backlot.planePos.y, backlot.planePos.x);
                            break;
                    }

                    outpt.Add(backlot);

                    y += height;

                }
            }
            return outpt;
        }
    }
}
#endif