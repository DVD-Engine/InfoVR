using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ChipBotBehavoiur : MonoBehaviour
{
    public GameObject bot_body;

    private GameObject PlayerObject;
    private Animator animcontroller;
    private XRSocketInteractor socket;

    // Update is called once per frame
    void Start()
    {
        animcontroller = GetComponent<Animator>();
        socket = GetComponentInChildren<XRSocketInteractor>();
    }
    void Update()
    {
        if (PlayerObject != null)
        {
            Vector3 eyesPos = PlayerObject.GetComponent<XROrigin>().Camera.transform.position;
            bot_body.transform.LookAt(eyesPos);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType() == typeof(CharacterController))
        {
            GameObject player = other.gameObject;
            PlayerObject = player;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == PlayerObject)
        {
            PlayerObject = null;
        }
    }

    public void change_state()
    {
        int state = 0;
        state = socket.interactablesSelected[0].transform.gameObject.GetComponent<ChipStateNumber>().ChipNumber;
        Debug.Log(state);
        animcontroller.SetInteger("State", state);
    }
    public void state_reset()
    {
        animcontroller.SetInteger("State", 0);
    }
}
