using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Market : Building, IBuilding
{
    public string BuildingName => "Market";
    public GameObject GameObject => gameObject;
    public Type Type => typeof(Market);

    public GameObject EntryPoint { get; set; }

    public BuildingData data
    {
        get { return base.GetData(); }
        set { data = value; }
    }


    void Start()
    {
        EntryPoint = gameObject.transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
