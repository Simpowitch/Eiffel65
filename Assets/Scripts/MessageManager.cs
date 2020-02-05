using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    #region Singleton
    public static MessageManager instance;
    private void Awake()
    {
        if (MessageManager.instance == null)
        {
            MessageManager.instance = this;
        }
        else
        {
            Debug.LogWarning("another instance of " + this + " was tried to be created, but is now destroyed from gameobject" + this.gameObject.name);
            Destroy(this);
        }
    }
    #endregion

    public Message debugMessage = null;

    private Message lastReceivedMessage;
    [SerializeField] TextMeshProUGUI senderNameText = null;
    [SerializeField] TextMeshProUGUI messageText = null;
    [SerializeField] Image screen = null;
    [SerializeField] Color screenColorOn = Color.blue;

    private void Start()
    {
        SetComputerStatus(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleComputer();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ReceiveMessage(debugMessage);
        }
    }

    public void ReceiveMessage(Message message)
    {
        Debug.Log("Receiving message from: " + message.sender);

        SetComputerStatus(true);

        //Save the received message
        lastReceivedMessage = message;

        //Display the sender's name
        senderNameText.text = message.sender;

        //Display message
        StartCoroutine(TypeSentence(message.message));
    }


    //Display the sentence letter by letter
    [SerializeField] float writeSpeed = 0.001f;
    IEnumerator TypeSentence(string sentence)
    {
        messageText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            messageText.text += letter;
            yield return new WaitForSeconds(writeSpeed);
        }
    }

    bool computerOn = false;
    public void ToggleComputer()
    {
        SetComputerStatus(!computerOn);
    }


    private void SetComputerStatus(bool on)
    {
        computerOn = on;
        if (on)
        {
            screen.color = screenColorOn;

            if (lastReceivedMessage != null)
            {
                //Display last message received
                senderNameText.text = lastReceivedMessage.sender;
                messageText.text = lastReceivedMessage.message;
            }
            Debug.Log("Computer turned on");
        }
        else
        {
            StopAllCoroutines();
            screen.color = Color.black;
            senderNameText.text = "";
            messageText.text = "";
            Debug.Log("Computer Closed");
        }
    }
}
