#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EvroDev.BacklotUtilities.Voxels;

namespace EvroDev.BacklotUtilities
{
    [InitializeOnLoad]
    public static class ShiftSelectionManager
    {
        static bool shiftDownPending;
        static double lastMouseDownTime;

        static HashSet<SelectableFace> _lastSelection = new HashSet<SelectableFace>();

        static ShiftSelectionManager()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnSelectionChanged()
        {
            var nowSelection = Selection.gameObjects.Where(g => g.TryGetComponent<SelectableFace>(out _)).Select(g => g.GetComponent<SelectableFace>()).ToHashSet();

            var added = new HashSet<SelectableFace>(nowSelection);
            added.ExceptWith(_lastSelection);

            var removed = new HashSet<SelectableFace>(_lastSelection);
            removed.ExceptWith(nowSelection);

            // Heuristic: if a left-click just happened with Shift down, the selection changed
            // within ~0.5s, and the selection only grew (added > 0, removed == 0),
            // we consider it a "Shift-select".
            bool recentClick = (EditorApplication.timeSinceStartup - lastMouseDownTime) < 0.5;
            bool selectionGrew = added.Count == 1 && removed.Count == 0;

            if (shiftDownPending && recentClick && selectionGrew)
            {
                SelectableFace newThing = added.Single();
                newThing.chunk.manager.ShiftSelect(newThing);
            }

            _lastSelection = nowSelection;
            shiftDownPending = false;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            var e = Event.current;
            if (e == null) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                shiftDownPending = e.shift;
                lastMouseDownTime = EditorApplication.timeSinceStartup;
            }
        }


    }
}
#endif