using System.Collections.Generic;
using UnityEngine;

namespace PeakTrollMod
{
    internal static class VisualCloneFactory
    {
        public static GameObject CloneVisuals(GameObject source, string name, Vector3 position, Quaternion rotation)
        {
            if (source == null) return null;
            Dictionary<Transform, Transform> map = new Dictionary<Transform, Transform>();
            GameObject root = new GameObject(name);
            root.transform.position = position; root.transform.rotation = rotation; root.transform.localScale = source.transform.lossyScale;
            map[source.transform] = root.transform;
            CopyTransforms(source.transform, root.transform, map);
            CopyRenderers(source, root, map);
            CopyAnimator(source, root, map);
            return root;
        }

        private static void CopyTransforms(Transform source, Transform target, Dictionary<Transform, Transform> map)
        {
            for (int i = 0; i < source.childCount; i++)
            {
                Transform child = source.GetChild(i);
                GameObject copy = new GameObject(child.name);
                copy.transform.SetParent(target, false);
                copy.transform.localPosition = child.localPosition; copy.transform.localRotation = child.localRotation; copy.transform.localScale = child.localScale;
                copy.SetActive(child.gameObject.activeSelf);
                map[child] = copy.transform;
                CopyTransforms(child, copy.transform, map);
            }
        }

        private static void CopyRenderers(GameObject source, GameObject target, Dictionary<Transform, Transform> map)
        {
            MeshFilter[] meshFilters = source.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Transform mapped; if (!map.TryGetValue(meshFilters[i].transform, out mapped)) continue;
                MeshFilter filter = mapped.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = meshFilters[i].sharedMesh;
                MeshRenderer original = meshFilters[i].GetComponent<MeshRenderer>();
                if (original != null)
                {
                    MeshRenderer renderer = mapped.gameObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = original.sharedMaterials; renderer.enabled = original.enabled;
                    renderer.shadowCastingMode = original.shadowCastingMode; renderer.receiveShadows = original.receiveShadows;
                }
            }
            SkinnedMeshRenderer[] skinned = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                Transform mapped; if (!map.TryGetValue(skinned[i].transform, out mapped)) continue;
                SkinnedMeshRenderer renderer = mapped.gameObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = skinned[i].sharedMesh; renderer.sharedMaterials = skinned[i].sharedMaterials; renderer.enabled = skinned[i].enabled;
                renderer.localBounds = skinned[i].localBounds; renderer.updateWhenOffscreen = true;
                Transform rootBone; if (skinned[i].rootBone != null && map.TryGetValue(skinned[i].rootBone, out rootBone)) renderer.rootBone = rootBone;
                Transform[] bones = new Transform[skinned[i].bones.Length];
                for (int b = 0; b < bones.Length; b++) { Transform bone; if (skinned[i].bones[b] != null && map.TryGetValue(skinned[i].bones[b], out bone)) bones[b] = bone; }
                renderer.bones = bones;
            }
        }

        private static void CopyAnimator(GameObject source, GameObject target, Dictionary<Transform, Transform> map)
        {
            Animator original = source.GetComponentInChildren<Animator>(true); if (original == null) return;
            Transform mapped; if (!map.TryGetValue(original.transform, out mapped)) return;
            Animator animator = mapped.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = original.runtimeAnimatorController; animator.avatar = original.avatar;
            animator.applyRootMotion = false; animator.updateMode = original.updateMode; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }
}
