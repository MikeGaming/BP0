﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SplineMesh {
    /// <summary>
    /// Example of component to bend many meshes along a spline. This component can be used as-is but will most likely be a base for your own component.
    /// 
    /// This is a more advanced and real-life SplineMesh component. Use it as a source of inspiration.
    /// 
    /// In this script, you will learn to : 
    ///  - preserve baked lightmap when entering playmode,
    ///  - better manage the generated content life cycle to avoid useless calculations
    ///  - create data class to produce richer content along your spline
    ///  
    /// This is the most complete Example provided in the asset. For further help, information and ideas, please visit
    /// the officiel thread on Unity forum.
    /// 
    /// And if you like SplineMesh, please review it on the asset store !
    /// 
    /// Now you should be able to bend the world to your will.
    /// 
    /// Have fun with SplineMesh !
    /// 
    /// </summary>
    [ExecuteInEditMode]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ExampleTrack : MonoBehaviour {
        private GameObject generated;
        private Spline spline = null;
        private bool toUpdate = false;

        /// <summary>
        /// A list of object that are storing data for each segment of the curve.
        /// </summary>
        public List<TrackSegment> segments = new List<TrackSegment>();

        /// <summary>
        /// If true, the generated content will be updated in play mode.
        /// If false, the content generated and saved to the scene will be used in playmode without modification.
        /// Usefull to preserve lightmaps baked for static objects.
        /// </summary>
        public bool updateInPlayMode;

        private void OnEnable() {
            string generatedName = "generated by " + GetType().Name;
            var generatedTranform = transform.Find(generatedName);
            generated = generatedTranform != null ? generatedTranform.gameObject : UOUtility.Create(generatedName, gameObject);

            spline = GetComponentInParent<Spline>();

            // we listen changes in the spline's node list and we update the list of segment accordingly
            // this way, if we insert a node between two others, a segment will be inserted too and the data won't shift
            while (segments.Count < spline.nodes.Count) {
                segments.Add(new TrackSegment());
            }
            while (segments.Count > spline.nodes.Count) {
                segments.RemoveAt(segments.Count - 1);
            }
            spline.NodeListChanged += (s, e) => {
                switch (e.type) {
                    case ListChangeType.Add:
                        segments.Add(new TrackSegment());
                        break;
                    case ListChangeType.Remove:
                        segments.RemoveAt(e.removeIndex);
                        break;
                    case ListChangeType.Insert:
                        segments.Insert(e.insertIndex, new TrackSegment());
                        break;
                }
                toUpdate = true;
            };
            toUpdate = true;
        }

        private void OnValidate() {
            if (spline == null) return;
            toUpdate = true;
        }

        private void Update() {
            // we can prevent the generated content to be updated during playmode to preserve baked data saved in the scene
            if (!updateInPlayMode && Application.isPlaying) return;

            if (toUpdate) {
                toUpdate = false;
                CreateMeshes();
            }
        }

        public void CreateMeshes() {
            List<GameObject> used = new List<GameObject>();

            for (int i = 0; i < spline.GetCurves().Count; i++) {
                var curve = spline.GetCurves()[i];
                foreach (var tm in segments[i].transformedMeshes) {
                    if (tm.mesh == null) {
                        // if there is no mesh specified for this segment, we ignore it.
                        continue;
                    }

                    // we try to find a game object previously generated. this avoids destroying/creating
                    // game objects at each update, wich is faster.
                    var childName = "segment " + i + " mesh " + segments[i].transformedMeshes.IndexOf(tm);
                    var childTransform = generated.transform.Find(childName);
                    GameObject go;
                    if (childTransform == null) {
                        go = UOUtility.Create(childName,
                            generated,
                            typeof(MeshFilter),
                            typeof(MeshRenderer),
                            typeof(MeshBender),
                            typeof(MeshCollider));
                        go.isStatic = true;
                    } else {
                        go = childTransform.gameObject;
                    }
                    go.GetComponent<MeshRenderer>().material = tm.material;
                    go.GetComponent<MeshCollider>().material = tm.physicMaterial;

                    // we update the data in the bender. It will decide itself if the bending must be recalculated.
                    MeshBender mb = go.GetComponent<MeshBender>();
                    mb.Source = SourceMesh.Build(tm.mesh)
                        .Translate(tm.translation)
                        .Rotate(Quaternion.Euler(tm.rotation))
                        .Scale(tm.scale);
                    mb.SetInterval(curve);
                    mb.ComputeIfNeeded();
                    used.Add(go);
                }
            }

            // finally, we destroy the unused objects
            foreach (var go in generated.transform
                .Cast<Transform>()
                .Select(child => child.gameObject).Except(used)) {
                UOUtility.Destroy(go);
            }
        }
    }

    /// <summary>
    /// This class store any data associated with a spline segment.
    /// In this example, a list of meshes.
    /// It is intended to be edited in the inspector.
    /// </summary>
    [Serializable]
    public class TrackSegment {
        public List<TransformedMesh> transformedMeshes = new List<TransformedMesh>();
    }

    /// <summary>
    /// This class stores all needed data to represent a mesh in situation.
    /// It is intended to be edited in the inspector.
    /// </summary>
    [Serializable]
    public class TransformedMesh {
        public TransformedMesh() {
            scale = Vector3.one;
        }
        public Mesh mesh;
        public Material material;
        public PhysicsMaterial physicMaterial;
        public Vector3 translation;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
    }
}
