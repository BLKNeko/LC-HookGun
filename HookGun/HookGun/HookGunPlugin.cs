using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using HookGun.Patches;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HookGun
{
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class HookGunPlugin : BaseUnityPlugin
    {

        private const string MODUID = "com.BLKNeko.HookGun";
        private const string MODNAME = "HookGun";
        private const string MODVERSION = "0.8.0.0";

        private readonly Harmony harmony = new Harmony(MODUID);

        private static HookGunPlugin Instance;

        internal ManualLogSource mls;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(MODUID);

            //mls.LogInfo("HookGun Awaken!");

            harmony.PatchAll(typeof(HookGunPlugin));
            harmony.PatchAll(typeof(AllowDeathPatch));
            harmony.PatchAll();

            Assets.LoadAssetBundle();
            //Assets.LoadSoundbank();
            Assets.PopulateAssets();



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

            Item TestHook = Assets.HGItem;

            HookGunScript HScript = TestHook.spawnPrefab.AddComponent<HookGunScript>();
            HScript.itemProperties = TestHook;

            Items.RegisterShopItem(TestHook, 0);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(TestHook.spawnPrefab);




        }



        public class HookGunScript : GrabbableObject
        {

            AudioSource audioSource;
            RoundManager roundManager;

            ///public PlayerControllerB[] players;

            public Transform gunTip;
            public LayerMask whatIsGrappleable = 1 << 6 | 1 << 8 | 1 << 9 | 1 << 11 | 1 << 12 | 1 << 20 | 1 << 25 | 1 << 26;
            public LineRenderer lr;

            public float maxGrappleDistance = 60f;
            public float grappleDelayTime = 1f;
            public float overshootYAxis = 1.5f;
            public float HookSpeed = 50f;

            public float HookTimer = 0;

            public float smoothTime = 1.3f;

            private bool grappling;

            public static bool NoDmg = false;

            private Vector3 grapplePoint;

            private Vector3 targetPosition;

            private Vector3 forces;



            //grabanimation: HoldLung

            //public override void Start()
            //{
            //    base.Start();
            //    gameObject.GetComponent<NetworkObject>().Spawn();
            //}


            public void Awake()
            {

                Debug.Log("Item aWAKE");

                audioSource = GetComponent<AudioSource>();
                roundManager = FindObjectOfType<RoundManager>();

                grabbable = true;
                grabbableToEnemies = true;
                useCooldown = 4f;
                insertedBattery = new Battery(false, 1);
                mainObjectRenderer = GetComponent<MeshRenderer>();
                //propBody = GetComponent<Rigidbody>();
                //radarIcon = GetComponent<Transform>();
                //propColliders = GetComponents<BoxCollider>();
                //propColliders = GetComponents<BoxCollider>();
                //propColliders = GetComponentsInParent<BoxCollider>();

                //component.tag PhysicsProp

                Debug.Log("Item this");
                Debug.Log(this);

                Debug.Log("Item grabbable");
                Debug.Log(grabbable);
                Debug.Log("Item grabbable enemy");
                Debug.Log(grabbableToEnemies);
                Debug.Log("Item main render");
                Debug.Log(mainObjectRenderer);
                //Debug.Log("Item propbody");
                //Debug.Log(propBody);
                //Debug.Log("Item radar icon");
                //Debug.Log(radarIcon);
                //Debug.Log("Item item Collider");
                //Debug.Log(propColliders);
                //
                //
                //Debug.Log("Item item properties");
                //Debug.Log(itemProperties);








            }

            public override void Update()
            {
                base.Update();

                if (grappling && targetPosition != null && targetPosition != Vector3.zero && !base.playerHeldBy.isPlayerDead)
                {
                    //Debug.Log("minVelocityToTakeDamage");
                    //Debug.Log(base.playerHeldBy.minVelocityToTakeDamage);
                    //base.playerHeldBy.minVelocityToTakeDamage = 9999f;
                    NoDmg = true;



                    if (HookTimer >= 6f)
                    {
                        grappling = false;
                        Invoke(nameof(backToNormal), 1f);
                        HookTimer = 0f;
                    }
                    else
                    {
                        HookTimer += Time.fixedDeltaTime;
                    }

                    if ((Vector3.Distance(base.playerHeldBy.transform.position, targetPosition) >= 0.5f))
                    {
                        HookSpeed = 50f;


                        forces = Vector3.Normalize(targetPosition - base.playerHeldBy.transform.position) * HookSpeed;

                        forces.y = forces.y * 1.5f;


                    }
                    else
                    {
                        HookSpeed = 0f;
                        forces = Vector3.zero;
                        grappling = false;
                        targetPosition = Vector3.zero;
                        Invoke(nameof(backToNormal), 3f);

                    }


                }

                playerHeldBy.externalForces += forces;


            }





            public override void ItemActivate(bool used, bool buttonDown = true)
            {
                base.ItemActivate(used, buttonDown);

                Debug.Log("Item active");


                if (insertedBattery.charge >= 0.1f)
                {
                    //insertedBattery.charge -= 0.1f;
                    if (insertedBattery.charge < 0.1f) insertedBattery.charge = 0;

                    //audioSource.PlayOneShot(shootSound);

                    StartGrapple();




                }


                // CROSSHAIR

                GameObject myObject = new GameObject("bar");
                //GameObject myObject = Modules.Assets.MoraleBar;



                myObject.transform.SetParent(base.playerHeldBy.gameplayCamera.transform);
                RectTransform rectTransform = myObject.AddComponent<RectTransform>();
                //rectTransform.anchorMin = Vector2.zero;
                //rectTransform.anchorMax = Vector2.one;
                rectTransform.anchorMin = new Vector2(0.57f, 0.7f);
                rectTransform.anchorMax = new Vector2(0.782f, 0.72f);
                //rectTransform.sizeDelta = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0.2f, 0.2f);
                //rectTransform.anchoredPosition = Vector2.zero;



                myObject.AddComponent<Image>();
                myObject.GetComponent<Image>().sprite = Assets.HGSprite;



            }


            //-------------

            private void StartGrapple()
            {
                //if (grapplingCdTimer > 0) return;

                Debug.Log("start graple");

                LineRenderer Trail = Instantiate(this, this.gameObject.transform.GetChild(0)).GetComponent<LineRenderer>();



                RaycastHit hit;
                if (Physics.Raycast(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, out hit, maxGrappleDistance, whatIsGrappleable))
                {
                    grapplePoint = hit.point;

                    Debug.Log("raycast hit");

                    Debug.Log("hit point");
                    Debug.Log(grapplePoint);
                    Debug.Log("distance");
                    Debug.Log(hit.distance);
                    Debug.Log("colliderid");
                    Debug.Log(hit.colliderInstanceID);

                    SpawnTrail(Trail, hit.point);

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



                }
                else
                {
                    //grapplePoint = transform.position + transform.forward * maxGrappleDistance;

                    Debug.Log("RatCast Miss");

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
                forces = Vector3.zero;
                grappling = false;
                targetPosition = Vector3.zero;
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





        class Assets
        {

            //-------------------ASSETS

            internal static AssetBundle mainAssetBundle;

            // CHANGE THIS
            private const string assetbundleName = "hookgunitem";

            private static string[] assetNames = new string[0];

            public static Item HGItem;

            public static Sprite HGSprite;

            public static Mesh HGMesh;
            public static Mesh HOMesh;

            internal static void LoadAssetBundle()
            {
                if (mainAssetBundle == null)
                {
                    using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetSkates." + assetbundleName))
                    {
                        mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                    }
                }

                assetNames = mainAssetBundle.GetAllAssetNames();
            }


            internal static void PopulateAssets()
            {
                if (!mainAssetBundle)
                {
                    Debug.LogError("There is no AssetBundle to load assets from.");
                    return;
                }


                HGSprite = mainAssetBundle.LoadAsset<Sprite>("HGSprite");

                HGItem = mainAssetBundle.LoadAsset<Item>("HookGunItem");


            }

        }


    }
}
