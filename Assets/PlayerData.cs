using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerData : MonoBehaviour
{
    public int health = 100;
    public int score = 0;
    public GameObject saveNotification;

    void Awake()
    {
        if (SaveSystem.SaveExists())
        {
            Load();
            Debug.Log("Save file found, loading...");
        }
        else
        {
            Debug.Log("No save file, starting fresh");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
        }
    }

    public void Save()
    {
        Debug.Log("Attempting to save to: " + Application.persistentDataPath);

        SaveData data = new SaveData();
        data.playerX = transform.position.x;
        data.playerY = transform.position.y;
        data.playerZ = transform.position.z;
        data.health = health;
        data.score = score;
        data.lastScene = SceneManager.GetActiveScene().name;

        SaveSystem.SaveGame(data);
        StartCoroutine(ShowSaveNotification());
    }

    IEnumerator ShowSaveNotification()
    {
        saveNotification.SetActive(true);
        yield return new WaitForSeconds(2f);
        saveNotification.SetActive(false);
    }

    public void Load()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null)
        {
            health = data.health;
            score = data.score;

            CharacterController cc = GetComponent<CharacterController>();
            cc.enabled = false;
            transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
            cc.enabled = true;

            Debug.Log("Loaded position: " + transform.position);
        }
    }
}