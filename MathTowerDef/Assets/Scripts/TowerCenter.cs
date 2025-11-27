//using UnityEngine;

//public class TowerCenter : MonoBehaviour {
//    [SerializeField] Transform[] _towerPositions;
//    Tower[] _towers;

//    public void initTowerCenter() {
//        _towers = new Tower[_towerPositions.Length];
//    }

//    public void createTower(int madeNumber) {
//        (int index, Vector3 emptyPosition) = getEmptyPosition();
//        if (index == -1) {
//            return;
//        }

//        GameObject monsterObj = ResourceManager.instantiateSync("Assets/Tower/Tower.prefab");
//        var tower = monsterObj.GetComponent<Tower>();
//        tower.setNumber(madeNumber);
//        tower.transform.position = emptyPosition;
//        _towers[index] = tower;
//    }

//    (int index, Vector3 pos) getEmptyPosition() {
//        for (int i = 0; i < _towerPositions.Length; i++) {
//            if (_towers[i] == null) {
//                return (i, _towerPositions[i].position);
//            }
//        }
//        return (-1, Vector3.zero);
//    }
//}
