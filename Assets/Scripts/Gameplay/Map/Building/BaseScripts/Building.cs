using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    public BuildingData GetData() { return buildingData; }
}
