using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MADNetworking;
using UnityEngine.UI;
using TMPro;

namespace MADNetworking
{
    public class ExampleGameManager : NetworkBehaviour
    {
        public Button playGameButton;
        public InputField playerNameInput;

        string opponentName = "";

        public Button startGameButton;
        public Text waitingForOpponent;
        public GameObject startingGround;
        public GameObject gamePlayingGround;
        public TextMeshProUGUI playerNameDisplay;
        public TextMeshProUGUI opponentNameDisplay;
        public TextMeshProUGUI matchCount;
        public TextMeshProUGUI finalResult;
        public GameObject selectionPage;
        public GameObject resultPage;
        public Sprite[] sprites;
        public Button[] selectionButtons;
        public Image selectionDisplay;
        public Image opponentSelectionDisplay;
        public Text whoGetsPoint;
        int selection = -1; //0=rock 1=paper 2=scissors
        int opponentSelection = -1; //0=rock 1=paper 2=scissors
        int _currentMatchCount = 0;
        int playerPoints;
        int opponentPoints;
        int currentMatchCount
        {
            get
            {
                return _currentMatchCount;
            }
            set
            {
                _currentMatchCount = value;
                matchCount.text = _currentMatchCount <=3 ? _currentMatchCount + "/" + "3" : "Game Over";
                NewMatch();
            }
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void NewMatch()
        {
            StartCoroutine(DelayMatchWait());
            
        }

        public void RPCGetSelection(int selection)
        {
            opponentSelection = selection;
        }
        public void Select(int selection)
        {
            this.selection = selection;
            foreach(var b in selectionButtons)
            {
                b.interactable = false;
            }
        }
        IEnumerator DelayMatchWait()
        {
            opponentSelection = -1;
            selection = -1;
            if (currentMatchCount <= 3)
            {
                selectionPage.SetActive(true);
            }
            resultPage.SetActive(false);
            foreach (var b in selectionButtons)
            {
                b.interactable = true;
            }
            yield return new WaitForSeconds(2f);
            while(selection == -1 || opponentSelection == -1)
            {
                yield return new WaitForSeconds(0.5f);
                NetworkInvoke("RPCGetSelection", new object[] { selection }, false);
            }

            selectionPage.SetActive(false);
            resultPage.SetActive(true);
            selectionDisplay.sprite = sprites[selection];
            opponentSelectionDisplay.sprite = sprites[opponentSelection];
            int winState = 0; //0 = lost, 1 = win, 2 = draw
            if(selection == opponentSelection)
            {
                winState = 2;
            }
            if(selection == 0)
            {
                //rock
                if(opponentSelection == 1)
                {
                    winState = 0;
                }
                else if(opponentSelection == 2)
                {
                    winState = 1;
                }
                
            }
            else if(selection == 1)
            {
                //paper
                if (opponentSelection == 0)
                {
                    winState = 1;
                }
                else if (opponentSelection == 2)
                {
                    winState = 0;
                }


            }
            else if (selection == 2)
            {
                //Scissors
                if (opponentSelection == 0)
                {
                    winState = 0;
                }
                else if (opponentSelection == 1)
                {
                    winState = 1;
                }


            }
            if(winState == 2)
            {
                whoGetsPoint.text = "Draw! Both players get point";

            }
            else if(winState == 0)
            {
                whoGetsPoint.text = opponentName + " Gets point";
                opponentPoints++;
            }
            else if(winState == 1)
            {
                whoGetsPoint.text = playerNameInput.text + " Gets point";
                playerPoints++;
            }

            yield return new WaitForSeconds(5);
            if (currentMatchCount == 3)
            {
                selectionPage.SetActive(false);
                finalResult.gameObject.SetActive(true);
                if(playerPoints == opponentPoints)
                {
                    finalResult.text = "Draw!";
                }
                else if(playerPoints < opponentPoints)
                {
                    finalResult.text = "You Lost!";
                }
                else
                {
                    finalResult.text = "You Win!";
                }
            }

            currentMatchCount++;

        }
        public void StartGame()
        {
            StartCoroutine(DelayConnection());
        }

        public void RPCGetUsername(string userName)
        {
            opponentName = userName;
        }

        IEnumerator DelayConnection()
        {
            startGameButton.interactable = false;
            waitingForOpponent.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            string username = playerNameInput.text != "" ? playerNameInput.text : "No Name";
            NetworkInvoke("RPCGetUsername", new object[] { username }, false);
            while (opponentName == "")
            {
                yield return new WaitForSeconds(0.5f);
                NetworkInvoke("RPCGetUsername", new object[] { username }, false);
            }

            EnterGame();
        }


        public void EnterGame()
        {
            startingGround.SetActive(false);
            gamePlayingGround.SetActive(true);
            opponentNameDisplay.text = opponentName;
            playerNameDisplay.text = playerNameInput.text;
            currentMatchCount++;
        }
    }
}

