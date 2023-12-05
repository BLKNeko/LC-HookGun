using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class download : MonoBehaviour
{


    void Start() {
        StartCoroutine(DownloadAssetBundle());
    }

    private IEnumerator DownloadAssetBundle(){

        GameObject go = null;

        string url = "https://drive.google.com/u/0/uc?id=1MGBPWMIVCS5JpPWWR6MUvLZeVacodT1g&export=download";

        using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(url)){

            yield return www.SendWebRequest();
            if(www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError){
                Debug.Log("erro");
            }
            else{
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(www);
                go = bundle.LoadAsset(bundle.GetAllAssetNames()[0]) as GameObject;
                bundle.Unload(false);
                yield return new WaitForEndOfFrame();
            }
            www.Dispose();

        }
        InstantiateGameObjectFromAssetBundle(go);

    }

    private void InstantiateGameObjectFromAssetBundle(GameObject go){
        if(go!=null){
            GameObject instanceGo = Instantiate(go);
            instanceGo.transform.position = Vector3.zero;
        }
        else{
            Debug.Log("Erro2");
        }
    }

}
