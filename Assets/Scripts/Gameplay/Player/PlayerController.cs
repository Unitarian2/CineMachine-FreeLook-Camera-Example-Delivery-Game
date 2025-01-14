using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;

    //Dependencies
    Grid grid;
    Unit playerUnit;
    GameManager gameManager;
    CineCameraManager cameraMan;

    Vector3 oldPos;
    Vector3 newPos;

    [SerializeField] private Transform returnPoint;

    private IBuilding deliveryStartBuilding;
    private IBuilding deliveryEndBuilding;

    public PlayerDeliveryType playerDeliveryState = PlayerDeliveryType.Waiting;

    

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
    }

    public void InitPlayerController(GameManager gameManager, CineCameraManager cameraMan)
    {
        this.gameManager = gameManager;
        this.cameraMan = cameraMan;
        cameraMan.StartDollies();
    }
    
    public void InitPlayerMapInfo(Grid grid, Unit playerUnit)
    {
        this.grid = grid;
        this.playerUnit = playerUnit;
        
    }
    /// <summary>
    /// Gidilecek ba�lang�� ve biti� rotas�n� olu�turur. newDelivery'de verilen bina tiplerine g�re se�imi yapar.
    /// </summary>
    /// <param name="newDelivery"></param>
    public void SetNewDelivery(DeliveryDestination newDelivery, UIManager uiManager)
    {
        Debug.LogWarning("Start : " + newDelivery.BuildingStart.BuildingName + " / "+ "End : "+ newDelivery.BuildingEnd.BuildingName);

        #region Start Route
        //Rotan�n ba�layaca�� bina belirleniyor. �nce yak�n civardaki binalarda arama yap�l�yor. Bulunamaz veya bir bug olu�ursa t�m binalarda arama yap�l�yor.
        BuildingFinder buildingFinderStart = new(newDelivery.BuildingStart, grid.GetNearbyBuildings(gameObject.transform.position));
        IBuilding closestBuildingStart = buildingFinderStart.FindSameBuildingsByType().FindClosestBuilding(gameObject.transform.position);
        if(closestBuildingStart == null)
        {
            Debug.Log("Building Not Found, Searching All Buildings");
            BuildingFinder buildingFinderStartAll = new(newDelivery.BuildingStart, grid.GetAllBuildings());
            closestBuildingStart = buildingFinderStartAll.FindSameBuildingsByType().FindClosestBuilding(gameObject.transform.position);
        }
        #endregion

        #region End Route
        //Rotan�n bitece�i bina belirleniyor. �nce yak�n civardaki binalarda arama yap�l�yor. Bulunamaz veya bir bug olu�ursa t�m binalarda arama yap�l�yor.
        BuildingFinder buildingFinderEnd = new(newDelivery.BuildingEnd, grid.GetNearbyBuildings(closestBuildingStart.EntryPoint.transform.position));
        IBuilding closestBuildingEnd = buildingFinderEnd.FindSameBuildingsByType().FindClosestBuilding(closestBuildingStart.EntryPoint.transform.position);
        if (closestBuildingEnd == null)
        {
            Debug.Log("Building Not Found, Searching All Buildings");
            BuildingFinder buildingFinderEndAll = new(newDelivery.BuildingEnd, grid.GetAllBuildings());
            closestBuildingEnd = buildingFinderEndAll.FindSameBuildingsByType().FindClosestBuilding(closestBuildingStart.EntryPoint.transform.position);
        }
        #endregion

        

        //Ba�lang�� ve biti� ba�ar�l� bir �ekilde bulunduysa hedefleri set ediyoruz. Bu if clause bug durumuna kar��n konuldu.
        if (closestBuildingStart != null && closestBuildingEnd != null)
        {
            //Yak�nda bina bulmu�uz
            Debug.Log("Route Found : "+ closestBuildingStart.GameObject.name + " to "+ closestBuildingEnd.GameObject.name);
            deliveryStartBuilding = closestBuildingStart;
            deliveryEndBuilding = closestBuildingEnd;

            uiManager.NewDeliveryDestinationArrived(new DeliveryDestination(deliveryStartBuilding,deliveryEndBuilding));
            StartToDeliver();
        }
        else
        {
            //Yak�nda bina bulamam���z. T�m haritada arayaca��z.
            Debug.Log("Building Not Found, Searching All Buildings");
            BuildingFinder buildingFinderStartAll = new(newDelivery.BuildingStart, grid.GetAllBuildings());
            deliveryStartBuilding = buildingFinderStartAll.FindSameBuildingsByType().FindClosestBuilding(gameObject.transform.position);

            BuildingFinder buildingFinderEndAll = new(newDelivery.BuildingEnd, grid.GetAllBuildings());
            deliveryEndBuilding = buildingFinderEndAll.FindSameBuildingsByType().FindClosestBuilding(deliveryStartBuilding.EntryPoint.transform.position);

            

            if (closestBuildingStart == null || closestBuildingEnd == null)
            {
                Debug.LogError("Invalid Route, Starting a New Delivery");
                gameManager.StartSingleDelivery();
            }
            else
            {
                Debug.Log("Route Found : " + closestBuildingStart.GameObject.name + " to " + closestBuildingEnd.GameObject.name);
                uiManager.NewDeliveryDestinationArrived(new DeliveryDestination(deliveryStartBuilding, deliveryEndBuilding));
                StartToDeliver();
            }
        }
    }

    /// <summary>
    /// Player Object rotaya ba�lar. Bunu �a��rmadan �nce SetNewDelivery ile rota belirleyin. Zaten bir ba�lang�� noktas�na gidiyorsa, onu bitirmeden yeni rotaya gitmez.
    /// </summary>
    public void StartToDeliver()
    {   
        if(playerDeliveryState != PlayerDeliveryType.MovingToStart)
        {  
            SetDestination(deliveryStartBuilding);
            playerDeliveryState = PlayerDeliveryType.MovingToStart;
            cameraMan.StopDollyProcess();
        }
        
    }

    /// <summary>
    /// Player Object'i bir binan�n Entry Point'ine g�nderir.
    /// </summary>
    /// <param name="buildingDestination"></param>
    private void SetDestination(IBuilding buildingDestination)
    {
        agent.SetDestination(buildingDestination.EntryPoint.transform.position);
    }


    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    Debug.Log("Mouse clicked");
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    RaycastHit hit;
        //    if(Physics.Raycast(ray,out hit))
        //    {
        //        agent.SetDestination(hit.point);
        //    }
        //}


        //Player'�n i�inde bulundu�u Grid Cell de�i�iminin kontrol�.
        oldPos = newPos;
        newPos = transform.position;
        grid.CheckPlayerMovement(playerUnit,oldPos,newPos);


    }

    void OnTriggerEnter(Collider other)
    {
        if(playerDeliveryState != PlayerDeliveryType.Waiting)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("DeliveryPoint"))
            {
                //Bir Teslimat Noktas�n�n s�n�rlar�na girmi�iz.
                if (other.gameObject.transform.parent.TryGetComponent<IBuilding>(out IBuilding building))
                {
                    if (building == deliveryStartBuilding && playerDeliveryState == PlayerDeliveryType.MovingToStart)
                    {
                        Debug.Log("Start Building'e ula��ld�!");
                        playerDeliveryState = PlayerDeliveryType.Waiting;
                        SetDestination(deliveryEndBuilding);
                        playerDeliveryState = PlayerDeliveryType.MovingToEnd;
                    }
                    else if (building == deliveryEndBuilding && playerDeliveryState == PlayerDeliveryType.MovingToEnd)
                    {
                        Debug.Log("End Building'e ula��ld�!");
                        playerDeliveryState = PlayerDeliveryType.Waiting;
                        //agent.isStopped = true;
                        gameManager.DeliveryCompleted(deliveryEndBuilding.Type);
                        ReturnToStartCenter();
                        //gameManager.StartSingleDelivery();
                    }
                }
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("CenterPoint"))
            {

                if (playerDeliveryState == PlayerDeliveryType.ReturningToCenter)
                {
                    //Ba�lang�� Noktas�na d�nm��
                    playerDeliveryState = PlayerDeliveryType.Waiting;
                    cameraMan.StartDollies();
                }

            }
            
            
        }
    }

    void ReturnToStartCenter()
    {
        playerDeliveryState = PlayerDeliveryType.ReturningToCenter;
        agent.SetDestination(returnPoint.position);
    }
   
}

public enum PlayerDeliveryType
{
    Waiting,
    MovingToStart,
    MovingToEnd,
    ReturningToCenter
}

