using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fragsurf.Movement;

public class Interactable : MonoBehaviour
{
    public GameObject promptText;
    public string dest_scene;
    public Vector3 dest_pos;
    SurfCharacter player;
    private bool playerInRange = false;
    
    void Awake()
    {
        player = FindObjectOfType<SurfCharacter>();
    }

    void Start()
    {
        if (promptText)
        {
            promptText.SetActive(false);
        }
    }
    void OnTriggerEnter(Collider collider)
    {
        if (gameObject.CompareTag("SceneTransition"))
        {
            SwitchSite();
        } else
        {
            promptText.SetActive(true);
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        promptText.SetActive(false);
        playerInRange = false;
    }

    public void SwitchSite()
    {
        SceneManager.LoadScene(dest_scene);
    }

    void Update()
    {
        if (playerInRange && player.InteractPressed && gameObject.CompareTag("InteractableSceneTransition"))
        {
            SwitchSite();
        }
        else if (playerInRange && player.InteractPressed && gameObject.CompareTag("Cup"))
        {
            // picked up cup!
            

            //change scenetransition to broken apartment


            gameObject.SetActive(false);
        }
    }
}
