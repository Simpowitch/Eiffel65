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


    private Message lastReceivedMessage;
    [SerializeField] TextMeshProUGUI senderNameText = null;
    [SerializeField] TextMeshProUGUI messageText = null;
    [SerializeField] Animator messagePanelAnimator = null;

    private void Start()
    {
        SetMessagePanelStatus(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleStatus();
        }
    }

    public void ReceiveMessage(Message message)
    {
        if (message == null)
        {
            return;
        }

        Debug.Log("Receiving message from: " + message.sender);

        SetMessagePanelStatus(true);

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

    bool panelOn = false;
    public void ToggleStatus()
    {
        SetMessagePanelStatus(!panelOn);
    }


    private void SetMessagePanelStatus(bool on)
    {
        panelOn = on;
        messagePanelAnimator.SetBool("Show", on);
        if (on)
        {
            if (lastReceivedMessage != null)
            {
                //Display last message received
                senderNameText.text = lastReceivedMessage.sender;
                messageText.text = lastReceivedMessage.message;
            }
            Debug.Log("Message panel opened");
        }
        else
        {
            StopAllCoroutines();
            senderNameText.text = "";
            messageText.text = "";
            Debug.Log("Message panel Closed");
        }
    }
}
