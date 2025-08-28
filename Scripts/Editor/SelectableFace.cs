#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EvroDev.BacklotUtilities.Extensions;
using UnityEditor;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;
using SLZ.Marrow.Warehouse;

namespace EvroDev.BacklotUtilities.Voxels
{
    public enum FaceDirection
    {
        Forward = 4,
        Backward = 5,
        Up = 1,
        Down = 0,
        Left = 2,
        Right = 3
    }

    public class SelectableFace : MonoBehaviour
    {
        public BacklotVoxelChunk chunk;
        public Vector3Int voxelPosition;
        public FaceDirection FaceDirection;
        public Material material;
        //public string surfaceData = "SLZ.Backlot.SurfaceDataCard.Concrete";
        public DataCardReference<SurfaceDataCard> surfaceData = new("SLZ.Backlot.SurfaceDataCard.Concrete");
        public bool IsEmpty = false;
        public MeshRenderer _renderer;
        public MeshFilter _filter;

        private static Mesh _gizmoMesh;
        private static Mesh gizmoMesh
        {
            get
            {
                if (_gizmoMesh == null)
                {
                    _gizmoMesh = new Mesh
                    {
                        name = "VoxelFaceGizmoMesh",
                        vertices = new Vector3[]
                        {
                            new Vector3(-0.45f, -0.45f, 0),
                            new Vector3(0.45f, -0.45f, 0),
                            new Vector3(0.45f, 0.45f, 0),
                            new Vector3(-0.45f, 0.45f, 0),
                            new Vector3(0.30f, -0.30f, 0),
                            new Vector3(-0.30f, -0.30f, 0),
                            new Vector3(-0.30f, 0.30f, 0),
                            new Vector3(0.30f, 0.30f, 0),
                        },
                        triangles = new int[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 },
                        normals = new Vector3[]
                        {
                            Vector3.forward,
                            Vector3.forward,
                            Vector3.forward,
                            Vector3.forward,
                            Vector3.back,
                            Vector3.back,
                            Vector3.back,
                            Vector3.back,
                        },
                        uv = new Vector2[]
                        {
                            new Vector2(0, 0),
                            new Vector2(1, 0),
                            new Vector2(1, 1),
                            new Vector2(0, 1),
                            new Vector2(0, 0),
                            new Vector2(1, 0),
                            new Vector2(1, 1),
                            new Vector2(0, 1),
                        }
                    };
                }
                return _gizmoMesh;
            }
        }

        private Vector3 gizmoScale
        {
            get
            {
                if(_gizmoScale == Vector3.zero)
                {
                    _gizmoScale = FaceDirection switch
                    {
                        FaceDirection.Up => new Vector3(1f, 0f, 1f),
                        FaceDirection.Down => new Vector3(1f, 0f, 1f),
                        FaceDirection.Forward => new Vector3(1f, 1f, 0f),
                        FaceDirection.Backward => new Vector3(1f, 1f, 0f),
                        FaceDirection.Right => new Vector3(0f, 1f, 1f),
                        FaceDirection.Left => new Vector3(0f, 1f, 1f),
                        _ => Vector3.one
                    };
                }
                return _gizmoScale;
            }
        }
        private Vector3 _gizmoScale;

        void OnDrawGizmos()
        {
            if (chunk == null) 
            {
                return;
            }

            if(chunk.manager.visualizationMode != VisualizationMode.Gizmos) return;

            transform.position = transform.parent.position + voxelPosition + (Vector3.one / 2);

            transform.position += FaceDirection switch 
            {
                FaceDirection.Up => new Vector3(0, 0.5f, 0),
                FaceDirection.Down => new Vector3(0, -0.5f, 0),
                FaceDirection.Forward => new Vector3(0, 0, 0.5f),
                FaceDirection.Backward => new Vector3(0, 0, -0.5f),
                FaceDirection.Right => new Vector3(0.5f, 0, 0),
                FaceDirection.Left => new Vector3(-0.5f, 0, 0),
                _ => Vector3.zero
            };

            Vector3 scale = FaceDirection switch 
            {
                FaceDirection.Up => new Vector3(0.9f, 0.01f, 0.9f),
                FaceDirection.Down => new Vector3(0.9f, 0.01f, 0.9f),
                FaceDirection.Forward => new Vector3(0.9f, 0.9f, 0.01f),
                FaceDirection.Backward => new Vector3(0.9f, 0.9f, 0.01f),
                FaceDirection.Right => new Vector3(0.01f, 0.9f, 0.9f),
                FaceDirection.Left => new Vector3(0.01f, 0.9f, 0.9f),
                _ => Vector3.zero
            };

            Quaternion rotation = FaceDirection switch 
            {
                FaceDirection.Up => Quaternion.Euler(90, 0, 0),
                FaceDirection.Down => Quaternion.Euler(-90, 0, 0),
                FaceDirection.Forward => Quaternion.Euler(0, 180, 0),
                FaceDirection.Backward => Quaternion.Euler(0, 0, 0),
                FaceDirection.Right => Quaternion.Euler(0, -90, 0),
                FaceDirection.Left => Quaternion.Euler(0, 90, 0),
                _ => Quaternion.identity
            };


            transform.localRotation = rotation;
            _renderer.sharedMaterial = material;

            if (IsEmpty)
            {
                _renderer.enabled = false;
                Gizmos.color = new Color(1, 1, 1, 0.1f);
                Gizmos.DrawCube(transform.position, scale);
            }
            else
            {
                _renderer.enabled = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (chunk == null) 
            {
                return;
            }
            if(chunk.manager.visualizationMode != VisualizationMode.Gizmos) return;

            Gizmos.color = new Color(1, 1, 1, 0.1f);
            //Gizmos.DrawCube(transform.position, gizmoScale);
            Gizmos.color = new Color(1, 1, 1, 0.5f);
            Gizmos.DrawWireCube(transform.position, gizmoScale);
        }

        public Vector3Int GetTargetAir()
        {
            return voxelPosition + GetAxis();
        }

        public Vector3Int GetBackwardPos()
        {
            return voxelPosition - GetAxis();
        }

        public void Extrude(bool add)
        {
            if(!add)
            {
                chunk.ExtrudeFaceGizmo(this);
            }
            else
            {
                chunk.IntrudeFaceGizmo(this);
            }
        }

        public Vector3Int GetAxis()
        {
            return FaceDirection switch
            {
                FaceDirection.Up => new Vector3Int(0, 1, 0),
                FaceDirection.Down => new Vector3Int(0, -1, 0),
                FaceDirection.Forward => new Vector3Int(0, 0, 1),
                FaceDirection.Backward => new Vector3Int(0, 0, -1),
                FaceDirection.Right => new Vector3Int(1, 0, 0),
                FaceDirection.Left => new Vector3Int(-1, 0, 0),
                _ => Vector3Int.zero
            };
        }

        public static SelectableFace Create(Transform parent, int x, int y, int z, FaceDirection direction, Voxel voxel, BacklotVoxelChunk chunk)
        {
            GameObject go = new GameObject("VoxelFace");
            SelectableFace outpt = go.AddComponent<SelectableFace>();
            outpt._filter = go.AddComponent<MeshFilter>();
            outpt._renderer = go.AddComponent<MeshRenderer>();
            outpt._filter.sharedMesh = gizmoMesh;
            outpt._filter.hideFlags = HideFlags.HideInInspector;
            outpt._renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            outpt._renderer.hideFlags = HideFlags.HideInInspector;
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            outpt.voxelPosition = new Vector3Int(x, y, z);
            outpt.FaceDirection = direction;

            if (voxel.GetMaterial(direction) != null)
                outpt.material = voxel.GetMaterial(direction);
            if (outpt.material == null)
                outpt.material = BacklotManager.DefaultGridMaterial();

            outpt.IsEmpty = voxel.GetOverrideFace(direction);

            var surfData = voxel.GetSurface(direction);
            if (surfData != null && surfData.IsValid())
                outpt.surfaceData = surfData;
            else
                voxel.SetSurface(direction, outpt.surfaceData);

                outpt.chunk = chunk;

            return outpt;
        }
    }

    [InitializeOnLoad]
    public class PlaneGizmoToolGUI
    {
        static PlaneGizmoToolGUI()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            var active = Selection.activeGameObject;
            if (active != null && active.TryGetComponent<SelectableFace>(out var face)) // You can change this condition
            {
                ToolManager.SetActiveTool<VoxelExtrudeTool>();
            }
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            // Filter selected objects to only those with PlaneGizmo
            var selectedPlanes = Selection.gameObjects
                .Select(go => go.GetComponent<SelectableFace>())
                .Where(p => p != null)
                .ToArray();

            if (selectedPlanes.Length == 0)
                return;

            // Calculate center position
            Vector3 center = Vector3.zero;
            foreach (var plane in selectedPlanes)
                center += plane.transform.position;
            center /= selectedPlanes.Length;

            // Convert world position to GUI position
            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(center);

            //Handles.BeginGUI();

            //GUILayout.BeginArea(new Rect(guiPosition.x - 50, guiPosition.y - 30, 100, 60), GUI.skin.box);

            Handles.color = Color.cyan;
            float size = HandleUtility.GetHandleSize(center + Vector3.up * 0.5f) * 0.2f;

            //if (Handles.Button(center + Vector3.up * 0.5f, Quaternion.identity, size, size, Handles.SphereHandleCap))
            //{
            //    ExtrudeSelectedPlanes(selectedPlanes, true);
            //}

            Handles.color = Color.red;
            float size2 = HandleUtility.GetHandleSize(center - Vector3.up * 0.5f) * 0.2f;

            //if (Handles.Button(center - Vector3.up * 0.5f, Quaternion.identity, size2, size2, Handles.SphereHandleCap))
            //{
            //    ExtrudeSelectedPlanes(selectedPlanes, false);
            //}

            //GUILayout.EndArea();
            //Handles.EndGUI();
        }

        static void ExtrudeSelectedPlanes(SelectableFace[] planes, bool add)
        {
            foreach (var plane in planes)
            {
                plane.Extrude(add); // Assume you have a method like this
            }

            SceneView.RepaintAll();
        }
    }
}
#endif