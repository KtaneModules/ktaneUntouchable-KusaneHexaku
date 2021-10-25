using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using Rnd = UnityEngine.Random;
using Newtonsoft.Json;

public class untouchableScript : MonoBehaviour {

	public KMAudio Audio;
	public KMBombModule Module;
	public KMBombInfo BombInfo;
	public TextMesh ScreenText;
	public KMSelectable GreenPaddle, BothPaddle, RedPaddle;
	public GameObject TimerBar;
	public MeshRenderer[] stageLights;
	public MeshRenderer[] handles;
	public Material ledOn;
	public Material ledOff;
	public Material selectedHandle;
	public Material unselectedHandle;

	//log moment
	static int _moduleIdCounter = 1;
	int _moduleId;
	private bool moduleSolved;

	private string[] paddleName = {"neither paddles", "the Green paddle", "the Red paddle", "both paddles"};

	private int seatAmount;
	private int yourSeat;
	private int calledNumber;
	private int calledModifier;
	private string screenDisplay;
	private bool playing;
	private int raisedPaddle;
	private int roundsPassed;
	private int pointerSeat;
	private int correctPaddle;
	private int seatRemoved;
	private int currentPaddle;
	private bool countingDown;
	private string onlyOrNot;
	private int modifierProbabilities;

	// Use this for initialization
	void Start () {

		_moduleId = _moduleIdCounter++;
		Debug.LogFormat ("[Untouchable #{0}] Welcome to Untouchable! Please, have a seat!", _moduleId);
		Debug.LogFormat ("[Untouchable #{0}] ...well, actually, we'd first need to figure out the seatings.", _moduleId);

		//calculate initial seats amount and the player's current seat

		modifierProbabilities = Rnd.Range (1, 6);

		seatAmount = (Convert.ToInt32(BombInfo.GetSerialNumberNumbers().LastOrDefault()) % 3) + 5;
		Debug.LogFormat ("[Untouchable #{0}] The last digit of the serial number is {1}, so there are a total of {2} initial seats.", _moduleId, Convert.ToInt32(BombInfo.GetSerialNumberNumbers().LastOrDefault()), seatAmount);

		yourSeat = 1 + (Convert.ToInt32 (BombInfo.GetSerialNumberNumbers().TakeLast(2).FirstOrDefault()) % seatAmount);
		Debug.LogFormat ("[Untouchable #{0}] And let's see... the third character of the serial number is a {1}...", _moduleId, BombInfo.GetSerialNumberNumbers().TakeLast(2).FirstOrDefault());
		Debug.LogFormat ("[Untouchable #{0}] ..alright! Sorry for the inconvenience, your seat is Seat #{1}. Good luck with the game!", _moduleId, yourSeat);

		ScreenText.text = "";

		GreenPaddle.OnInteract += delegate () {
				raisedPaddle = 1;
				ProcessPaddle();
			return false;
		};

		RedPaddle.OnInteract += delegate () {
				raisedPaddle = 2;
				ProcessPaddle();
			return false;
		};

		BothPaddle.OnInteract += delegate () {
				raisedPaddle = 3;
				ProcessPaddle();
			return false;
		};


	}

	private IEnumerator RoundStart()
	{

		playing = true;
		Debug.LogFormat ("[Untouchable #{0}] A round has been started!", _moduleId);
		roundsPassed = 0;
		raisedPaddle = 0;
		pointerSeat = 1;
		raisedPaddle = 0;

		while (playing)
		{
			countingDown = true;
			calledNumber = Rnd.Range (1, seatAmount + 3);

			calledModifier = Rnd.Range (modifierProbabilities, 14);

			correctPaddle = 0;

			Debug.LogFormat ("[Untouchable #{0}] The pointer is currently at Seat #{1}.", _moduleId, pointerSeat);

			if (calledModifier == 11) {
				Debug.LogFormat ("[Untouchable #{0}] The number called is -{1}, meaning we are counting from right to left this time.", _moduleId, calledNumber);
				ScreenText.text = "-"+calledNumber.ToString ();
			}
			if (calledModifier == 12) {
				Debug.LogFormat ("[Untouchable #{0}] The number called is !{1}, meaning red and green swap functions this time.", _moduleId, calledNumber);
				ScreenText.text = "!"+calledNumber.ToString ();
			} 
			if (calledModifier == 13) {
				Debug.LogFormat ("[Untouchable #{0}] The number called is {1}>, meaning the red paddle skips one seat over this time.", _moduleId, calledNumber);
				ScreenText.text = calledNumber.ToString ()+">";
			} 
			if (calledModifier == 14) {
				Debug.LogFormat ("[Untouchable #{0}] The number called is [{1}], meaning green paddles do not get raised this time", _moduleId, calledNumber);
				ScreenText.text = "["+calledNumber.ToString ()+"]";
			}
			if (calledModifier < 11) {
				Debug.LogFormat ("[Untouchable #{0}] The number called is {1}, with no rule modifer.", _moduleId, calledNumber);
				ScreenText.text = calledNumber.ToString ();
			}

			if (calledModifier != 14)
			{
				if (calledNumber >= seatAmount)
					correctPaddle = 1;

				if (calledModifier == 11)
				{
					for (int i = 0; i < calledNumber; i++)
					{
						if (pointerSeat == yourSeat)
							correctPaddle = 1;
						
						pointerSeat--;

						if (pointerSeat == 0)
							pointerSeat = seatAmount;
					}
				}
				else
				{
					for (int i = 0; i < calledNumber; i++)
					{
						if (pointerSeat == yourSeat)
							correctPaddle = 1;
						
						pointerSeat++;

						if (pointerSeat > seatAmount)
							pointerSeat = 1;
					}

					if (calledModifier == 13)
					{
						pointerSeat++;
						if (pointerSeat > seatAmount)
							pointerSeat = 1;
					}
						
				}
			}

			if (pointerSeat == yourSeat)
				correctPaddle = correctPaddle + 2;

			if (calledModifier == 12)
			{
				if (correctPaddle == 1)
				{
					correctPaddle = 2;
				}
				else
				{
					if (correctPaddle == 2)
					{
						correctPaddle = 1;
					}
				}
			}


			StartCoroutine (Countdown ());
			yield return new WaitUntil (() => countingDown == false);

			TimerBar.gameObject.transform.localScale = new Vector3 (0.15f, 0.005f, 0.01f);
			TimerBar.gameObject.transform.localPosition = new Vector3 (0f, 0.0076939f, 0.0324f);
			onlyOrNot = " ";

			Debug.LogFormat ("[Untouchable #{0}] The pointer have moved to Seat #{1}.", _moduleId, pointerSeat);
			Debug.Log (paddleName [correctPaddle]);

			if (correctPaddle == raisedPaddle)
			{
				Audio.PlaySoundAtTransform ("correct", Module.transform);
				stageLights[roundsPassed].material = ledOn;
				roundsPassed++;
				Debug.LogFormat ("[Untouchable #{0}] You raised {1}, correct! Call {2} of 6 completed.", _moduleId, paddleName [raisedPaddle], roundsPassed);
			}
			else
			{
				Module.HandleStrike ();

				if (raisedPaddle == 3)
					onlyOrNot = " only ";

				Debug.LogFormat ("[Untouchable #{0}] You raised {1}, incorrect! You should have raised{2}{3}!", _moduleId, paddleName [raisedPaddle], onlyOrNot, paddleName [correctPaddle]);
				Debug.LogFormat ("[Untouchable #{0}] A strike has been handed out, and the round reset.", _moduleId);
				ScreenText.text = "";
				playing = false;
			}

			if (roundsPassed == 6)
			{
				Debug.LogFormat ("[Untouchable #{0}] Round complete!", _moduleId);

				seatAmount--;
				seatRemoved = Rnd.Range (1, seatAmount);
				if (seatRemoved == yourSeat)
				{
					seatRemoved++;
					if (seatRemoved > seatAmount)
						seatRemoved = 1;
				}

				if (seatRemoved < yourSeat)
				{
					yourSeat--;
					Debug.LogFormat ("[Untouchable #{0}] The player in Seat #{1} has been eliminated, your seat is now #{2}", _moduleId, seatRemoved, yourSeat);
				}
				else
				{
					Debug.LogFormat ("[Untouchable #{0}] The player in Seat #{1} has been eliminated, your seat is still #{2}", _moduleId, seatRemoved, yourSeat);
				}

				if (seatAmount == 1) {
					Debug.LogFormat ("[Untouchable #{0}] You are now the only player remaining! Congratulation! Module solved.", _moduleId);
					Audio.PlaySoundAtTransform ("solve", Module.transform);
					moduleSolved = true;
					StartCoroutine(SolveAnimation());
				} else {
					ScreenText.text = "#" + seatRemoved.ToString ();
					Audio.PlaySoundAtTransform ("roundcomplete", Module.transform);
				}
				playing = false;
			}

			raisedPaddle = 0;
			currentPaddle = 0;
			handles [0].material = unselectedHandle;
			handles [1].material = unselectedHandle;
			handles [2].material = unselectedHandle;

		}
		playing = false;
		for (int q = 0; q < 6; q++)
		{
			stageLights [q].material = ledOff;
		}
		raisedPaddle = 0;
		currentPaddle = 0;
		handles [0].material = unselectedHandle;
		handles [1].material = unselectedHandle;
		handles [2].material = unselectedHandle;

	}

	IEnumerator Countdown()
	{
		float smooth = 10;
		float deltaWidth = 0.15f / (4.9f * smooth);
		float deltaX = deltaWidth/2;
		float currentWidth = 0.15f;
		float currentX = 0f;
		Audio.PlaySoundAtTransform ("timer", Module.transform);
		for (int i = 1; i <= 4.9f * smooth; i++)
		{
				TimerBar.gameObject.transform.localScale = new Vector3 (currentWidth, 0.005f, 0.01f);
				TimerBar.gameObject.transform.localPosition = new Vector3 (currentX, 0.0076939f, 0.0324f);
				currentWidth -= deltaWidth;
				currentX -= deltaX;
				yield return new WaitForSeconds (1f / smooth);
		}
		TimerBar.gameObject.transform.localScale = new Vector3(0.15f, 0.005f, 0.01f);
		TimerBar.gameObject.transform.localPosition = new Vector3(0f, 0.0076939f, 0.0324f);
		countingDown = false;
		yield return null;
	}

	private IEnumerator SolveAnimation()
	{
		for (int i = 0; i < 26; i++)
		{
			string[] letterRand = {"0","1","2","3","4","5","6","7","8","9"};
			ScreenText.text =  letterRand[Rnd.Range(0,10)];
			yield return new WaitForSeconds (0.03f);
		}
		ScreenText.text = "<>";
		Module.HandlePass();
	}

	private void ProcessPaddle()
	{
		if(moduleSolved)
			return;
		
		if (!playing)
		{

			currentPaddle = 0;
			handles [0].material = unselectedHandle;
			handles [1].material = unselectedHandle;
			handles [2].material = unselectedHandle;

			StartCoroutine (RoundStart ());
		}
		else
		{
			if (raisedPaddle == currentPaddle)
			{
				raisedPaddle = 0;
				currentPaddle = 0;
				handles [0].material = unselectedHandle;
				handles [1].material = unselectedHandle;
				handles [2].material = unselectedHandle;
				Audio.PlaySoundAtTransform ("deselect", Module.transform);
			}
			else
			{
				currentPaddle = raisedPaddle;
				handles [0].material = unselectedHandle;
				handles [1].material = unselectedHandle;
				handles [2].material = unselectedHandle;
				handles [raisedPaddle - 1].material = selectedHandle;
				Audio.PlaySoundAtTransform ("select", Module.transform);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {




	}
}
