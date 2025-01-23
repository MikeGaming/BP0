using UnityEngine;

public class GravityPointSpawner : MonoBehaviour
{
    [SerializeField] GameObject _meshObject;
    private Mesh _mesh;
    [SerializeField] private GameObject _gravityPointPrefab;
    [SerializeField] private float _gravityPointDistance = 1f;
    private bool triggered;

    private void Update()
    {
        if (!triggered)
        {
            SpawnGravityPointsAfterStart();
            triggered = true;
        }
    }

    private void SpawnGravityPointsAfterStart()
    {
        _mesh = _meshObject.GetComponent<MeshFilter>().mesh;

        // Use normals of the mesh to spawn gravity points, so that their forward (blue) vectors are facing outwards (in the direction opposite to the normals).
        // Also make sure they are being spawned in the local space of the mesh object and parented to it, with the correct orientation too.
        for (int i = 0; i < _mesh.normals.Length; i++)
        {
            Vector3 localNormal = _mesh.normals[i];
            Vector3 worldNormal = _meshObject.transform.TransformDirection(localNormal);
            Vector3 localPosition = _mesh.vertices[i] - localNormal * _gravityPointDistance;
            Vector3 worldPosition = _meshObject.transform.TransformPoint(localPosition);
            Quaternion spawnRotation = Quaternion.LookRotation(-worldNormal, _meshObject.transform.up);
            Instantiate(_gravityPointPrefab, worldPosition, spawnRotation, _meshObject.transform);
        }
    }
}
