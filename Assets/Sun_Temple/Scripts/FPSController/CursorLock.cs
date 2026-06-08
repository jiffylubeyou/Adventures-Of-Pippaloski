using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace Sun_Temple{

	public class CursorLock : MonoBehaviour {

		private bool isLocked;

		void Start(){
			isLocked = true;
		}


	

		void Update(){
			
#if ENABLE_INPUT_SYSTEM
			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
#else
			if (Input.GetKeyDown (KeyCode.Escape)) {
#endif
				if (isLocked) {
					isLocked = false;
				} else if (!isLocked) {
					isLocked = true;
				}
			}



			if (isLocked) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}

			if (!isLocked) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

			


			
	
	}

}
