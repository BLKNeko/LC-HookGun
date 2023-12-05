using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class AssetBundleLoader : MonoBehaviour
{
    public string assetBundlePath;

    void Start()
    {
        StartCoroutine(LoadAssetBundle());
    }

    IEnumerator LoadAssetBundle()
    {
        AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        if (assetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle");
            yield break;
        }

        // Aqui, você pode instanciar ou fazer qualquer operação com os GameObjects do AssetBundle.
        GameObject prefab = assetBundle.LoadAsset<GameObject>("shotgun");
        Instantiate(prefab);

        // Carregue o ScriptableObject do AssetBundle
        //Item exemploObject = assetBundle.LoadAsset<Item>("shotgun");

        // Instancie o objeto (você pode fazer o que quiser com ele a partir daqui)
        // instancia = Instantiate(exemploObject);

        //Debug.Log(instancia);
        

        assetBundle.Unload(false);
    }
}
