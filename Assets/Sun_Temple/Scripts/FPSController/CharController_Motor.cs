using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SunTemple{


	public class CharController_Motor : MonoBehaviour {

		public float speed = 10.0f;
		public float sensitivity = 60.0f;
		CharacterController character;
		public GameObject cam;
		float moveFB, moveLR;	
		float rotHorizontal, rotVertical;
		public bool webGLRightClickRotation = true;
		float gravity = -9.8f;

		//string debugText;


		void Start(){

			character = GetComponent<CharacterController> ();

			webGLRightClickRotation = false;

			if (Application.platform == RuntimePlatform.WebGLPlayer) {
				webGLRightClickRotation = true;
				sensitivity = sensitivity * 1.5f;
			}


		}





		void FixedUpdate(){
#if ENABLE_INPUT_SYSTEM
			var kb = Keyboard.current;
			var mouse = Mouse.current;
			moveFB = (kb != null ? ((kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f)) : 0f) * speed;
			moveLR = (kb != null ? ((kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f)) : 0f) * speed;
			rotHorizontal = (mouse != null ? mouse.delta.ReadValue().x : 0f) * sensitivity;
			rotVertical   = (mouse != null ? mouse.delta.ReadValue().y : 0f) * sensitivity;
#else
			moveFB = Input.GetAxis ("Horizontal") * speed;
			moveLR = Input.GetAxis ("Vertical") * speed;
			rotHorizontal = Input.GetAxisRaw ("Mouse X") * sensitivity;
			rotVertical = Input.GetAxisRaw ("Mouse Y") * sensitivity;
#endif

			Vector3 movement = new Vector3 (moveFB, gravity, moveLR);

			if (webGLRightClickRotation) {
#if ENABLE_INPUT_SYSTEM
				if (Mouse.current != null && Mouse.current.leftButton.isPressed) {
#else
				if (Input.GetKey (KeyCode.Mouse0)) {
#endif
					CameraRotation (cam, rotHorizontal, rotVertical);
				}
			} else if (!webGLRightClickRotation) {
				CameraRotation (cam, rotHorizontal, rotVertical);
			}

			movement = transform.rotation * movement;
			character.Move (movement * Time.fixedDeltaTime);
		}





	

		void CameraRotation(GameObject cam, float rotHorizontal, float rotVertical){	

			transform.Rotate (0, rotHorizontal * Time.fixedDeltaTime, 0);
			cam.transform.Rotate (-rotVertical * Time.fixedDeltaTime, 0, 0);



			if (Mathf.Abs (cam.transform.localRotation.x) > 0.7) {

				float clamped = 0.7f * Mathf.Sign (cam.transform.localRotation.x); 

				Quaternion adjustedRotation = new Quaternion (clamped, cam.transform.localRotation.y, cam.transform.localRotation.z, cam.transform.localRotation.w);
				cam.transform.localRotation = adjustedRotation;
			}


		}




	}



}