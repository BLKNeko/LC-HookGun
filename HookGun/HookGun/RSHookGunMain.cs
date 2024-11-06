using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using HookGun.Patches;
using LethalLib.Modules;
using ReservedItemSlotCore.Config;
using ReservedItemSlotCore.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace HookGun
{
    [BepInDependency(PluginInfo.MODUID3, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("FlipMods.ReservedItemSlotCore", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginInfo.MODUID2, PluginInfo.MODNAME2, PluginInfo.MODVERSION)]
    public class RSHookGunMain : BaseUnityPlugin
    {

        private readonly Harmony harmony = new Harmony(PluginInfo.MODUID);

        private static RSHookGunMain Instance;

        internal ManualLogSource mls;

        public static ReservedItemSlotData HGRSSlotData;
        public static ReservedItemData HGRSData;
        public static List<ReservedItemData> HGRSadditionalItemData = new List<ReservedItemData>();

        //public static object HGRSSlotData;
        //public static object HGRSData;
        //public static List<object> HGRSadditionalItemData;


        //private Item IHookGun;
        private Item IRSHookGun;



        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.MODUID);

            //mls.LogInfo("HookGun Awaken!");


            // netcode patching stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }



 

            //Debug.Log("HGItem");
            //Debug.Log(Assets.HGItem);

            //Item TestHook = Assets.HGItem;

            //HookGunScript HScript = TestHook.spawnPrefab.AddComponent<HookGunScript>();
            //HScript.itemProperties = TestHook;

            //Items.RegisterShopItem(TestHook, itemPrice.Value);
            //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(TestHook.spawnPrefab);

            //IHookGun = Assets.HGItem;

            //HookGunScript HScript = IHookGun.spawnPrefab.AddComponent<HookGunScript>();
            //HScript.itemProperties = IHookGun;

            //Items.RegisterShopItem(IHookGun, itemPrice.Value);
            //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(IHookGun.spawnPrefab);

            // This will register your ReservedItemSlotData and will be added to the game if the host is running the mod
            // Arguments: ItemSlotName (format is not too important), ItemSlotPriority, ItemSlotPrice
            //ReservedItemSlotData HGReservedItemSlot = ReservedItemSlotData.CreateReservedItemSlotData("HGRS", 23, itemPrice.Value);

            // Create your ReservedItemData for your item
            // You can also add the extra arguments to allow this item to be shown on players while holstered. (refer to the previous example)
            //ReservedItemData HGReservedItemData = new ReservedItemData("HookGunRS");

            // Add the ReservedItemData to your ReservedItemSlotData and you're done!
            //HGReservedItemSlot.AddItemToReservedItemSlot(HGReservedItemData);

            IRSHookGun = HookGunPlugin.Assets.HGItemRS;
            //HookRS.itemName = "HookGunRS";


            HookGunScript HScriptRS = IRSHookGun.spawnPrefab.AddComponent<HookGunScript>();
            HScriptRS.itemProperties = IRSHookGun;

            //HookRS.itemName = "RSHookGun";
            //HookRS.spawnPrefab.name = "RSHookGun";
            Items.RegisterShopItem(IRSHookGun, HookGunPlugin.itemPrice.Value);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(IRSHookGun.spawnPrefab);


            CreateReservedItemSlots();

            //Debug.Log("HG SP");
            //Debug.Log(IHookGun.spawnPrefab);
            //Debug.Log("HGRS SP");
            //Debug.Log(IRSHookGun.spawnPrefab);



        }

        void CreateReservedItemSlots()
        {
            HGRSSlotData = ReservedItemSlotData.CreateReservedItemSlotData("RSHookGun", 23, HookGunPlugin.itemPrice.Value);
            HGRSData = HGRSSlotData.AddItemToReservedItemSlot(new ReservedItemData("RSHookGun", PlayerBone.RightHand, new Vector3(-.2f, .25f, 0f), new Vector3(0, 90, 90)));
        }






        public class HookGunScript : GrabbableObject
        {

            AudioSource audioSource;
            RoundManager roundManager;

            MeshRenderer HookGO;

            ///public PlayerControllerB[] players;

            //public Transform gunTip;
            //public LayerMask whatIsGrappleable = 1 << 6 | 1 << 8 | 1 << 9 | 1 << 11 | 1 << 12 | 1 << 20 | 1 << 21 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27 | 1 << 28;
            //public LayerMask whatIsGrappleable;
            public LayerMask whatIsGrappleable = 1 << 8 | 1 << 9 | 1 << 12 | 1 << 15 | 1 << 25 | 1 << 26 | 1 << 27;
            public LayerMask whatIsGrappleableToPull = 1 << 6 | 1 << 20;
            public LayerMask whatIsGrappleableNew = 1 << 8 | 1 << 9 | 1 << 11 | 1 << 14 | 1 << 16 | 1 << 17 | 1 << 21 | 1 << 24 | 1 << 25;

            // 0 - various random objects
            // 8 - floor
            // 9 - stair


            public LineRenderer lr;

            public float maxGrappleDistance = 60f;
            public float grappleDelayTime = 1f;
            public float overshootYAxis = 1.5f;
            public float HookSpeed = 48f;
            public float OkDistance = 1f;
            public float HookmaxTimer = 6f;

            public float HookTimer = 0;

            public float smoothTime = 1.3f;

            public static bool grappling = false;
            public static bool pulling = false;
            public static bool shoudFall;
            public static bool DisableJump;
            public static bool DisableFall;
            private bool grapplingSlowY = false;

            private bool JumpNoDmg = true;

            public static bool isJumping = false;

            public static bool NoDmg = false;


            private Vector3 grapplePoint = Vector3.zero;

            private Vector3 targetPosition = Vector3.zero;

            private Vector3 forces = Vector3.zero;

            private Vector3 forcesP = Vector3.zero;

            private GameObject pullingGameObject;

            RaycastHit hit;



            //grabanimation: HoldLung


            public void Awake()
            {

                //Debug.Log("Item aWAKE");

                //HookGO = GetComponentInChildren<MeshRenderer>();

                audioSource = GetComponent<AudioSource>();
                roundManager = FindObjectOfType<RoundManager>();

                grabbable = true;
                grabbableToEnemies = true;
                useCooldown = HookGunPlugin.itemCooldown.Value;
                insertedBattery = new Battery(false, 1);
                mainObjectRenderer = GetComponent<MeshRenderer>();


                //Debug.Log("Item this");
                //Debug.Log(this);

                //Debug.Log("Item grabbable");
                //Debug.Log(grabbable);
                //Debug.Log("Item grabbable enemy");
                //Debug.Log(grabbableToEnemies);
                //Debug.Log("Item main render");
                //Debug.Log(mainObjectRenderer);








            }

 

            public override void Update()
            {
                base.Update();

                //verticaloffset = 0.4f

                //if (base.playerHeldBy.currentlyHeldObject.itemProperties.itemId == 84048)
                //{
                //    Debug.Log("Item HookGun on hand ----------");
                //
                //
                //}

                if (this.isHeld)
                {


                    if (this.currentUseCooldown <= 0 && insertedBattery.charge > 0)
                    {
                        this.gameObject.transform.Find("HookMesh").gameObject.active = true;
                    }
                    else
                    {
                        this.gameObject.transform.Find("HookMesh").gameObject.active = false;
                    }


                    if (pulling && targetPosition != null && targetPosition != Vector3.zero && !base.playerHeldBy.isPlayerDead)
                    {

                            //pullingGameObject.transform.position = Vector3.MoveTowards(targetPosition, base.playerHeldBy.transform.position, maxGrappleDistance);

                            //pullingGameObject.GetComponent<GrabbableObject>().targetFloorPosition = base.playerHeldBy.transform.position;

                            pullingGameObject.GetComponent<GrabbableObject>().transform.position = base.playerHeldBy.transform.position + new Vector3(0.2f,0.8f,0.2f);

                            pullingGameObject.GetComponent<GrabbableObject>().FallToGround(false);




                            HookTimer = 0f;
                            HookSpeed = 0f;
                            forcesP = Vector3.zero;
                            pulling = false;
                            targetPosition = Vector3.zero;
                            Invoke(nameof(backToNormal), 0.5f);



                    }




                    //grapling -----------------------------

                    if (grappling && targetPosition != null && targetPosition != Vector3.zero && !base.playerHeldBy.isPlayerDead)
                    {


                        if (!base.playerHeldBy.isInsideFactory)
                            NoDmg = true;



                        if (HookTimer >= HookmaxTimer)
                        {
                            //grappling = false;
                            Invoke(nameof(backToNormal), 1f);
                            Invoke(nameof(enableDamage), 4f);
                            HookTimer = 0f;
                            HookSpeed = 0f;
                            forces = Vector3.zero;
                            grappling = false;
                            targetPosition = Vector3.zero;
                            playerHeldBy.ResetFallGravity();
                            playerHeldBy.averageVelocity = 0f;
                            playerHeldBy.externalForces = Vector3.zero;
                        }
                        else
                        {
                            HookTimer += Time.fixedDeltaTime;
                        }

                        if ((Vector3.Distance(base.playerHeldBy.transform.position, targetPosition) >= OkDistance))
                        {



                            if (base.playerHeldBy.isInsideFactory)
                                HookSpeed = 38f;
                            else
                                HookSpeed = 45f;


                            forces = Vector3.Normalize(targetPosition - base.playerHeldBy.transform.position) * HookSpeed;

                            if (base.playerHeldBy.isInsideFactory)
                                forces.y = forces.y * 2f;
                            else
                                forces.y = forces.y * 1.5f;

                            playerHeldBy.externalForces.x += forces.x;
                            playerHeldBy.externalForces.z += forces.z;
                            playerHeldBy.externalForces.y += forces.y;



                        }
                        else
                        {

                            //Debug.Log("Reach >>>>>>>>");
                            HookTimer = 0f;
                            HookSpeed = 0f;
                            forces = Vector3.zero;
                            grappling = false;
                            targetPosition = Vector3.zero;
                            Invoke(nameof(backToNormal), 2f);
                            Invoke(nameof(enableDamage), 8f);
                            playerHeldBy.ResetFallGravity();
                            playerHeldBy.averageVelocity = 0f;
                            playerHeldBy.externalForces = Vector3.zero;
                            //playerHeldBy.TeleportPlayer(targetPosition);
                            //playerHeldBy.StartCoroutine("PlayerJump");

                        }


                    }


                    




                    //-----------
                }



                


            }



            public override void ItemActivate(bool used, bool buttonDown = true)
            {
                base.ItemActivate(used, buttonDown);

                //Debug.Log("Item active");

                if (base.playerHeldBy.isInsideFactory)
                {
                    OkDistance = 2f;
                    HookmaxTimer = 4f;
                }
                else
                {
                    OkDistance = 1f;
                    HookmaxTimer = 6f;
                }
                    




                if (insertedBattery.charge >= HookGunPlugin.energyCost.Value)
                {
                    insertedBattery.charge -= HookGunPlugin.energyCost.Value;
                    if (insertedBattery.charge < HookGunPlugin.energyCost.Value) insertedBattery.charge = 0;

                    //audioSource.PlayOneShot(shootSound);
                    audioSource.PlayOneShot(HookGunPlugin.Assets.ShootSFX);
                    StartGrapple();
                    //TestGrapple();





                }
                else
                    audioSource.PlayOneShot(HookGunPlugin.Assets.NoAmmoSFX);


                // CROSSHAIR

                //GameObject myObject = new GameObject("bar");
                ////GameObject myObject = Modules.Assets.MoraleBar;
                //
                //
                //
                //myObject.transform.SetParent(base.playerHeldBy.gameplayCamera.transform);
                //RectTransform rectTransform = myObject.AddComponent<RectTransform>();
                ////rectTransform.anchorMin = Vector2.zero;
                ////rectTransform.anchorMax = Vector2.one;
                //rectTransform.anchorMin = new Vector2(0.57f, 0.7f);
                //rectTransform.anchorMax = new Vector2(0.782f, 0.72f);
                ////rectTransform.sizeDelta = Vector2.zero;
                //rectTransform.sizeDelta = new Vector2(0.2f, 0.2f);
                ////rectTransform.anchoredPosition = Vector2.zero;
                //
                //
                //
                //myObject.AddComponent<Image>();
                //myObject.GetComponent<Image>().sprite = Assets.HGSprite;
                //


            }

            /*
             
            1 << 8 | 1 << 9 | 1 << 11 | 1 << 14 | 1 << 16 | 1 << 17 | 1 << 21 | 1 << 24 | 1 << 25;

            [Info   : Unity Log] O jogador colide com a camada TransparentFX 
            [Info   : Unity Log] O jogador colide com a camada Ignore Raycast
            [Info   : Unity Log] O jogador NÃO colide com a camada Player
            [Info   : Unity Log] O jogador colide com a camada Water
            [Info   : Unity Log] O jogador colide com a camada UI
            [Info   : Unity Log] O jogador NÃO colide com a camada Props
            [Info   : Unity Log] O jogador NÃO colide com a camada HelmetVisor
            [Info   : Unity Log] O jogador colide com a camada Room
            [Info   : Unity Log] O jogador colide com a camada InteractableObject
            [Info   : Unity Log] O jogador NÃO colide com a camada Foliage
            [Info   : Unity Log] O jogador colide com a camada Colliders 11
            [Info   : Unity Log] O jogador NÃO colide com a camada PhysicsObject 12
            [Info   : Unity Log] O jogador colide com a camada Triggers
            [Info   : Unity Log] O jogador NÃO colide com a camada MapRadar
            [Info   : Unity Log] O jogador NÃO colide com a camada NavigationSurface
            [Info   : Unity Log] O jogador colide com a camada RoomLight
            [Info   : Unity Log] O jogador colide com a camada Anomaly 17
            [Info   : Unity Log] O jogador NÃO colide com a camada LineOfSight
            [Info   : Unity Log] O jogador colide com a camada Enemies 19
            [Info   : Unity Log] O jogador NÃO colide com a camada PlayerRagdoll
            [Info   : Unity Log] O jogador colide com a camada MapHazards 21
            [Info   : Unity Log] O jogador NÃO colide com a camada ScanNode
            [Info   : Unity Log] O jogador colide com a camada EnemiesNotRendered
            [Info   : Unity Log] O jogador colide com a camada MiscLevelGeometry
            [Info   : Unity Log] O jogador colide com a camada Terrain
            [Info   : Unity Log] O jogador NÃO colide com a camada PlaceableShipObjects
            [Info   : Unity Log] O jogador NÃO colide com a camada PlacementBlocker
             
             */


            //-------------

            private void TestGrapple()
            {

                LineRenderer Trail = Instantiate(this, this.gameObject.transform.GetChild(0)).GetComponent<LineRenderer>();

                Debug.DrawRay(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, Color.green, 10f);

                if (Physics.Raycast(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, out hit, maxGrappleDistance, whatIsGrappleableNew))
                {
                    SpawnTrail(Trail, hit.point);

                }

                for(int i = 0; i < 48; i++)
                {
                    if (Physics.GetIgnoreLayerCollision(3, i))
                    {
                        Debug.Log("O jogador NÃO colide com a camada " + LayerMask.LayerToName(i));
                    }
                    else
                    {
                        Debug.Log("O jogador colide com a camada " + LayerMask.LayerToName(i));
                    }
                }



            }



            private void StartGrapple()
            {
                //if (grapplingCdTimer > 0) return;

                //Debug.Log("start graple");

                LineRenderer Trail = Instantiate(this, this.gameObject.transform.GetChild(0)).GetComponent<LineRenderer>();

                //whatIsGrappleable = base.playerHeldBy.GetComponent<LayerMask>();

                //Debug.Log("LayerMask:");

                //Debug.Log(whatIsGrappleable);

                //RaycastHit hit;

                



                if (Physics.Raycast(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, out hit, maxGrappleDistance, whatIsGrappleableToPull))
                {
                    grapplePoint = hit.point;

                    SpawnTrail(Trail, hit.point);

                    //Debug.Log("hit gameobject layer");
                    //Debug.Log(hit.transform.gameObject.layer);


                    targetPosition = grapplePoint;


                    Vector3 position = base.playerHeldBy.transform.position;
                    Vector3 offsetToHit = position - hit.point;
                    offsetToHit.Normalize();
                    offsetToHit *= 1;
                    position = hit.point + offsetToHit;

                    position.y = position.y * 1.1f;

                    targetPosition = position;

                    pulling = true;

                    pullingGameObject = hit.transform.gameObject;


                    audioSource.PlayOneShot(HookGunPlugin.Assets.HitSFX);

                    //Debug.Log("PULLING");

                }
                else if (Physics.Raycast(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, out hit, maxGrappleDistance, whatIsGrappleable))
                {
                    grapplePoint = hit.point;

                    //Debug.Log("raycast hit");
                    //
                    //Debug.Log("hit point");
                    //Debug.Log(grapplePoint);
                    //Debug.Log("distance");
                    //Debug.Log(hit.distance);
                    //Debug.Log("colliderid");
                    //Debug.Log(hit.colliderInstanceID);

                    SpawnTrail(Trail, hit.point);

                    //Debug.Log("hit gameobject layer");
                    //Debug.Log(hit.transform.gameObject.layer);

                    //Invoke(nameof(ExecuteGrapple), grappleDelayTime);

                    targetPosition = grapplePoint;

                    //------------TELEPORT --------
                    //Vector3 position = base.playerHeldBy.transform.position;
                    //Vector3 offsetToHit = position - hit.point;
                    //offsetToHit.Normalize();
                    //offsetToHit *= 1;
                    //position = hit.point + offsetToHit;
                    //base.playerHeldBy.transform.position = new Vector3(position.x, position.y, position.z);

                    Vector3 position = base.playerHeldBy.transform.position;
                    Vector3 offsetToHit = position - hit.point;
                    offsetToHit.Normalize();
                    offsetToHit *= 1;
                    position = hit.point + offsetToHit;

                    position.y = position.y * 1.1f;

                    targetPosition = position;
                    grappling = true;

                    audioSource.PlayOneShot(HookGunPlugin.Assets.HitSFX);

                }
                else
                {
                    //grapplePoint = transform.position + transform.forward * maxGrappleDistance;

                    //Debug.Log("RatCast Miss");

                    audioSource.PlayOneShot(HookGunPlugin.Assets.MissSFX);

                    Trail.gameObject.AddComponent<LineRendererFadeOut>();

                    insertedBattery.charge += HookGunPlugin.energyCost.Value;

                    //SpawnTrail(Trail, hit.point);

                }

            }

            private void SpawnTrail(LineRenderer Trail, Vector3 HitPoint)
            {
                Trail.SetPositions(new Vector3[2] { this.gameObject.transform.GetChild(0).position, HitPoint });
                Trail.gameObject.AddComponent<LineRendererFadeOut>();
            }


            private void backToNormal()
            {
                HookSpeed = 0f;
                HookTimer = 0f;
                forces = Vector3.zero;
                grappling = false;
                targetPosition = Vector3.zero;
                playerHeldBy.ResetFallGravity();
                playerHeldBy.averageVelocity = 0f;
                playerHeldBy.externalForces = Vector3.zero;


            }

            private void enableDamage()
            {
                NoDmg = false;

            }


        }




        public class LineRendererFadeOut : MonoBehaviour
        {
            public LineRenderer lineRenderer;
            public float fadeDuration = 1f;
            public float destroyDelay = 0.5f;

            private float elapsed = 0f;
            private bool isFading = true;

            void Start()
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            void Update()
            {
                if (isFading)
                {
                    if (elapsed < fadeDuration)
                    {
                        float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

                        lineRenderer.material.color = new Color(lineRenderer.material.color.r, lineRenderer.material.color.g, lineRenderer.material.color.b, alpha);

                        elapsed += Time.deltaTime;
                    }
                    else
                    {
                        lineRenderer.material.color = new Color(lineRenderer.material.color.r, lineRenderer.material.color.g, lineRenderer.material.color.b, 0f);

                        isFading = false;

                        Destroy(gameObject, destroyDelay);
                    }
                }
            }

        }





    }
}
