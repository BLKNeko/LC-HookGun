﻿using BepInEx;
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
        private const string MODVERSION = "1.0.0.0";

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

            Items.RegisterShopItem(TestHook, 25);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(TestHook.spawnPrefab);




        }



        public class HookGunScript : GrabbableObject
        {

            AudioSource audioSource;
            RoundManager roundManager;

            MeshRenderer HookGO;

            ///public PlayerControllerB[] players;

            public Transform gunTip;
            //public LayerMask whatIsGrappleable = 1 << 6 | 1 << 8 | 1 << 9 | 1 << 11 | 1 << 12 | 1 << 20 | 1 << 21 | 1 << 24 | 1 << 25 | 1 << 26 | 1 << 27 | 1 << 28;
            //public LayerMask whatIsGrappleable;
            public LayerMask whatIsGrappleable = 1 << 0 | 1 << 8 | 1 << 9 | 1 << 12 | 1 << 15 | 1 << 25 | 1 << 26 | 1 << 27;
            public LayerMask whatIsGrappleableToPull = 1 << 6 | 1 << 20;
            public LineRenderer lr;

            public float maxGrappleDistance = 60f;
            public float grappleDelayTime = 1f;
            public float overshootYAxis = 1.5f;
            public float HookSpeed = 48f;
            public float OkDistance = 0.5f;
            public float HookmaxTimer = 6f;

            public float HookTimer = 0;

            public float smoothTime = 1.3f;

            public static bool grappling = false;
            public static bool shoudFall;
            public static bool DisableJump;
            public static bool DisableFall;
            private bool grapplingSlowY = false;

            public static bool NoDmg = false;

            private Vector3 grapplePoint = Vector3.zero;

            private Vector3 targetPosition = Vector3.zero;

            private Vector3 forces = Vector3.zero;



            //grabanimation: HoldLung


            public void Awake()
            {

                //Debug.Log("Item aWAKE");

                HookGO = GetComponentInChildren<MeshRenderer>();

                audioSource = GetComponent<AudioSource>();
                roundManager = FindObjectOfType<RoundManager>();

                grabbable = true;
                grabbableToEnemies = true;
                useCooldown = 3f;
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


                if (grappling && targetPosition != null && targetPosition != Vector3.zero && !base.playerHeldBy.isPlayerDead)
                {


                    if (!base.playerHeldBy.isInsideFactory)
                        NoDmg = true;

                    

                    if (HookTimer >= HookmaxTimer)
                    {
                        //grappling = false;
                        Invoke(nameof(backToNormal), 1f);
                        Invoke(nameof(enableDamage), 2f);
                        HookTimer = 0f;
                    }
                    else
                    {
                        HookTimer += Time.fixedDeltaTime;
                    }

                    if ((Vector3.Distance(base.playerHeldBy.transform.position, targetPosition) >= OkDistance))
                    {

                        

                        if (base.playerHeldBy.isInsideFactory)
                            HookSpeed = 40f;
                        else
                            HookSpeed = 48f;


                        forces = Vector3.Normalize(targetPosition - base.playerHeldBy.transform.position) * HookSpeed;

                        if (base.playerHeldBy.isInsideFactory)
                            forces.y = forces.y * 2f;
                        else
                            forces.y = forces.y * 1.5f;


                    }
                    else
                    {

                        //Debug.Log("Reach >>>>>>>>");
                        HookSpeed = 0f;
                        forces = Vector3.zero;
                        grappling = false;
                        targetPosition = Vector3.zero;
                        Invoke(nameof(backToNormal), 3f);
                        Invoke(nameof(enableDamage), 5f);

                    }


                }


                playerHeldBy.externalForces.x += forces.x;
                playerHeldBy.externalForces.z += forces.z;
                playerHeldBy.externalForces.y += forces.y;


            }



            public override void ItemActivate(bool used, bool buttonDown = true)
            {
                base.ItemActivate(used, buttonDown);

                Debug.Log("Item active");

                if (base.playerHeldBy.isInsideFactory)
                {
                    OkDistance = 2f;
                    HookmaxTimer = 4f;
                }
                else
                {
                    OkDistance = 0.5f;
                    HookmaxTimer = 6f;
                }
                    




                if (insertedBattery.charge >= 0.05f)
                {
                    insertedBattery.charge -= 0.05f;
                    if (insertedBattery.charge < 0.05f) insertedBattery.charge = 0;

                    //audioSource.PlayOneShot(shootSound);
                    audioSource.PlayOneShot(Assets.ShootSFX);
                    StartGrapple();




                }
                else
                    audioSource.PlayOneShot(Assets.NoAmmoSFX);


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

                //Debug.Log("start graple");

                LineRenderer Trail = Instantiate(this, this.gameObject.transform.GetChild(0)).GetComponent<LineRenderer>();

                //whatIsGrappleable = base.playerHeldBy.GetComponent<LayerMask>();

                //Debug.Log("LayerMask:");

                //Debug.Log(whatIsGrappleable);

                RaycastHit hit;
                if (Physics.Raycast(base.playerHeldBy.gameplayCamera.transform.position, base.playerHeldBy.gameplayCamera.transform.forward, out hit, maxGrappleDistance, whatIsGrappleable))
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

                    //Invoke(nameof(ExecuteGrapple), grappleDelayTime);

                    targetPosition = grapplePoint;

                    HookGO.transform.position = targetPosition;


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

                    audioSource.PlayOneShot(Assets.HitSFX);

                }
                else
                {
                    //grapplePoint = transform.position + transform.forward * maxGrappleDistance;

                    //Debug.Log("RatCast Miss");

                    audioSource.PlayOneShot(Assets.MissSFX);

                    Trail.gameObject.AddComponent<LineRendererFadeOut>();

                    insertedBattery.charge += 0.05f;

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





        class Assets
        {

            //-------------------ASSETS

            internal static AssetBundle mainAssetBundle;

            // CHANGE THIS
            private const string assetbundleName = "hookgunitem";

            private static string[] assetNames = new string[0];

            public static Item HGItem;

            public static Sprite HGSprite;

            public static AudioClip ShootSFX;
            public static AudioClip HitSFX;
            public static AudioClip MissSFX;
            public static AudioClip NoAmmoSFX;

            public static Mesh HGMesh;
            public static Mesh HOMesh;

            internal static void LoadAssetBundle()
            {
                if (mainAssetBundle == null)
                {
                    using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HookGun." + assetbundleName))
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

                ShootSFX = mainAssetBundle.LoadAsset<AudioClip>("ShootSFX");
                HitSFX = mainAssetBundle.LoadAsset<AudioClip>("HitSFX");
                MissSFX = mainAssetBundle.LoadAsset<AudioClip>("MissSFX");
                NoAmmoSFX = mainAssetBundle.LoadAsset<AudioClip>("NoAmmoSFX");

                HGItem = mainAssetBundle.LoadAsset<Item>("HookGunItem");


            }

        }


    }
}
