using UnityEngine;

public class MonsterRunway : MonoBehaviour {
    [SerializeField] Transform[] _runways;
    [SerializeField] Vector2[] _runwayPoints;
    [SerializeField] float[] _runwayDistances;
    [SerializeField] float _totalDistance;

    private void OnValidate() {
        if(_runways == null || _runways.Length <= 1) {
            return;
        }

        _runwayPoints = new Vector2[_runways.Length + 1];
        _runwayDistances = new float[_runways.Length];

        for (int i = 0; i < _runways.Length + 1; i++) {
            int index = i % _runways.Length;
            _runwayPoints[i] = new Vector2(_runways[index].position.x, _runways[index].position.z);
        }

        _totalDistance = 0f;
        for (int i = 0; i < _runwayDistances.Length; i++) {
            int nextIndex = (i + 1) % _runwayPoints.Length;
            _runwayDistances[i] = Vector2.Distance(_runwayPoints[i], _runwayPoints[nextIndex]);
            _totalDistance += _runwayDistances[i];
        }
    }

    public Vector2 getPositionByDistance(float currentDistance) {
        currentDistance = Mathf.Repeat(currentDistance, _totalDistance);
        float ratio = currentDistance / _totalDistance;
        return getPositionByRatio(ratio);
    }

    public Vector2 getPositionByRatio(float ratio) {
        ratio = Mathf.Repeat(ratio, 1f);
        float targetDistance = ratio * _totalDistance;
        float accumulatedDistance = 0f;
        for (int i = 0; i < _runwayDistances.Length; i++) {
            if (accumulatedDistance + _runwayDistances[i] >= targetDistance) {
                float segmentRatio = (targetDistance - accumulatedDistance) / _runwayDistances[i];
                int nextIndex = (i + 1) % _runwayDistances.Length;
                Vector2 position2D = Vector2.Lerp(_runwayPoints[i], _runwayPoints[nextIndex], segmentRatio);
                return position2D;
            }
            accumulatedDistance += _runwayDistances[i];
        }
        // Fallback (should not reach here)
        return _runwayPoints[0];
    }
}
