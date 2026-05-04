using UnityEngine;

public class minimapManager : MonoBehaviour
{
    public buttonMinimap[] buttons;

    public roomController[] rooms;
    //public string isActiveRoom = "";
    //public string currentState = "";
    
    void Start()
    {
    //    Invoke(nameof(startsStateMachine), 20f);
    }
    
    /*
    void startsStateMachine()
    {
        StateMachine("start");
    }
    */

    //State Machine for The Mini Map
    public void StateMachineMiniMap(string state)
    {
        //currentState = state;

        //Debug.Log("Was clicked");

        switch (state)
        {
            case "start":
                activateMinimapRoom("br1_button");
                bringRealRoom("RoomBR1");
                showsPinMap("br1_button");
                break;

            case "finish_br1":
                activateMinimapRoom("pr1_button");
                activateMinimapRoom("pr2_button");
                break;

            case "finish_pr1":
                activateMinimapRoom("rr1_button");
                break;

            case "finish_rr1":
                // TODO: Check victory / No new rooms
                break;

            case "finish_br2":
                activateMinimapRoom("rr2_button");
                break;

            case "finish_pr2":
                activateMinimapRoom("br2_button");
                break;

            case "finish_rr2":
                // TODO: Check victory / No new rooms
                break;

            default:
                Debug.LogWarning("Unknown state: " + state);
                break;
        }
    }

    public void activateMinimapRoom(string buttonName)
    {
        // Activate new mini map room
        foreach (buttonMinimap btn in buttons)
        {
            if (btn.gameObject.name == buttonName)
            {
                btn.openCover(); // call function from the OTHER script
                //isActiveRoom = buttonName;
                break;
            }
        }
    }

    //State Machine for the Whole Rooms
    public void StateMachineRooms(string state)
    {
        //currentState = state;

        //Debug.Log("Was clicked");

        switch (state)
        {
            case "bringRoomBR1":
                bringRealRoom("RoomBR1");
                showsPinMap("br1_button");
                break;

            case "bringRoomBR2":
                bringRealRoom("RoomBR2");
                showsPinMap("br2_button");
                break;

            case "bringRoomPR1":
                bringRealRoom("RoomPR1");
                showsPinMap("pr1_button");
                break;

            case "bringRoomPR2":
                bringRealRoom("RoomPR2");
                showsPinMap("pr2_button");
                break;

            case "bringRoomRR1":
                bringRealRoom("RoomRR1");
                showsPinMap("rr1_button");
                break;

            case "bringRoomRR2":
                bringRealRoom("RoomRR2");
                showsPinMap("rr2_button");
                break;

            default:
                Debug.LogWarning("Unknown state: " + state);
                break;
        }
    }

    public void bringRealRoom(string roomName)
    {
        // Activate big Room
        foreach (roomController room in rooms)
        {
            if (room.gameObject.name == roomName)
            {
                //Shows Room
                room.showRoom();
            }
            else
            {
                //Hides Room
                room.hideRoom();
            }
        }
    }

    public void showsPinMap(string buttonName)
    {
        // Activate Pin Map
        foreach (buttonMinimap btn in buttons)
        {
            if (btn.gameObject.name == buttonName)
            {
                //Shows PinMap
                btn.ShowPin();
            }
            else
            {
                //Hides PinMap
                btn.HidePin();
            }
        }
    }

}