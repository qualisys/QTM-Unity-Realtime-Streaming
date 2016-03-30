﻿#region --- LICENSE ---
/*
    The MIT License (MIT)
    
    Copyright (c) 2016 Eduard Wolf
    
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using UnityEngine;
using UVR = UnityEngine.VR;

using QualisysRealTime.Unity.Skeleton;

namespace QualisysRealTime.Unity.VR
{
    public class OVRIntegration : MonoBehaviour
    {

        #region Script settings

        // Camera rig transform of eye cameras
        public GameObject cameraRig;

        // Offset vector for camera rig
        public Vector3 cameraOffset = new Vector3(0, 0.11f, 0.11f);

        // Use internal tracker of rift
        public bool useInternalTracking = true;

        // Use 6dof rigid body definition
        public bool useRiftRigidBody = true;

        //
        public string bodyName = "OculusRift";

        // Show a mirrored avatar
        public bool showMirror = false;

        #endregion

        #region Private properties

        // Camera rig component
        private OVRCameraRig ovrCameraRig;

        // ...
        private CharacterGameObjects characterJoints;
        private CharacterGameObjects mirroredCharacterJoints;

        // Avatar mesh renderes
        private Component[] renderers;

        // Orientation of 6dof rigid body
        private Quaternion sixDOFBodyRotation = Quaternion.identity;
        private float offsetAngle = 0;

        // Camera rigs needs reorientation
        private bool needsCameraRigReorientation = true;

        // Eye anchor transform
        private Transform eyeAnchor;

        #endregion

        /// <summary>
        /// Initialization on enable.
        /// </summary>
        public void OnEnable()
        {
            if (cameraRig == null)
                return;

            if (ovrCameraRig == null)
            {
                ovrCameraRig = cameraRig.GetComponent<OVRCameraRig>() as OVRCameraRig;
            }
            if (ovrCameraRig != null)
            {
                //ovrCameraRig.OrientationUpdated += riftOrientationChanged;
            }
            if (renderers == null)
            {
                renderers = this.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
            if (eyeAnchor == null)
            {
                eyeAnchor = cameraRig.transform;
            }
            characterJoints = new CharacterGameObjects();
        }

        /// <summary>
        /// Disable.
        /// </summary>
        public void OnDisable()
        {
            if (ovrCameraRig != null)
            {
                //ovrCameraRig.OrientationUpdated -= riftOrientationChanged;
            }
        }

        // Use this for initialization
        void Start()
        {
            // Cancel if no camera found
            if (ovrCameraRig == null)
                return;

            // Find and set character joints
            characterJoints.SetLimbs(this.transform, false);

            // Create mirrored Avatar
            if (showMirror)
            {
                // Copy avatar
                GameObject avatar = this.transform.gameObject;
                GameObject mirroredAvatar = Instantiate(avatar) as GameObject;
                mirroredAvatar.SetActive(false);

                // Delete unnecessary components
                foreach (Component component in mirroredAvatar.GetComponents<Component>())
                {
                    if (!(component is Transform))
                        Destroy(component);
                }

                mirroredCharacterJoints = new CharacterGameObjects();
                mirroredCharacterJoints.SetLimbs(mirroredAvatar.transform, false);
            }

            /*
			// Disable avatars mesh as long the camera rig is not orientated
			if (useInternalTracking) {
				foreach (SkinnedMeshRenderer renderer in renderers) {
					renderer.enabled = false;
				}
			}
            */

            // Attach camera rig to characters head (eyes) (if no internal tracking used)
            if (!useInternalTracking)
            {
                Transform head = characterJoints.head;
                cameraRig.transform.parent = head;
                cameraRig.transform.localPosition = cameraOffset;
                sixDOFBodyRotation = head.localRotation;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (ovrCameraRig == null)
                return;

            // ...
        }

        /// <summary>
        /// ...
        /// </summary>
        void LateUpdate()
        {
            // Cancel if no camera rig found
            if (ovrCameraRig == null)
                return;

            // Get streamed rigid body definition of rift
            if (useRiftRigidBody && RTClient.GetInstance().GetStreamingStatus())
            {
                SixDOFBody rift = RTClient.GetInstance().GetBody(bodyName);
                if (rift != null && !rift.Rotation.Convert().IsNaN())
                {
                    sixDOFBodyRotation = new Quaternion(rift.Rotation.x, rift.Rotation.y, rift.Rotation.z, rift.Rotation.w);

                    // Reorientate the camera rig to match the 6 DOF coordinate system
                    if (
                        useInternalTracking &&
                        needsCameraRigReorientation &&
                        sixDOFBodyRotation != Quaternion.identity
                    )
                    {
                        Debug.Log("Calibrating Oculus Rift Pose...");
                        Debug.Log("Rift rotation " + rift.Rotation.eulerAngles);
                        Debug.Log("Anchor rotation " + eyeAnchor.localEulerAngles);

                        // Align the camera rig coordinate system to the QTM coordinate system by a rotation over the y-axis
                        offsetAngle = eyeAnchor.localEulerAngles.y + sixDOFBodyRotation.eulerAngles.y + 180; // 180 cause of the rigid body axes
                        //(360 - eyeAnchor.eulerAngles.y) + sixDOFBodyRotation.eulerAngles.y;

                        // Enable avatar mesh
                        foreach (SkinnedMeshRenderer renderer in renderers)
                        {
                            renderer.enabled = true;
                        }

                        // OVR global coordinate system is calibrated
                        needsCameraRigReorientation = false;
                    }

                    // Show mirrored avatar when camera rig is orientated
                    if (showMirror)
                    {
                        mirroredCharacterJoints.root.gameObject.SetActive(true);
                    }

                    //RTClient.GetInstance().SetStatusMessage("OCULUS_RIFT", "", true);
                }
                else
                {
                    //RTClient.GetInstance().SetStatusMessage("OCULUS_RIFT", "", false);
                }
            }
            else if (!useRiftRigidBody && showMirror)
            {
                mirroredCharacterJoints.root.gameObject.SetActive(true);
            }

            // Get avatars head
            Transform head = characterJoints.head;
            Vector3 lookAt = eyeAnchor.forward * 10.0f;

            // Set head orientation to rift orientation and position the camera rig in front of the head
            if (useInternalTracking)
            {
                cameraRig.transform.eulerAngles = new Vector3(0, this.transform.eulerAngles.y + offsetAngle, 0);
                head.rotation = eyeAnchor.rotation;
                cameraRig.transform.position = head.position + cameraRig.transform.rotation * cameraOffset;
                //cameraRig.transform.position = head.position + head.rotation * cameraOffset;
            }
            else
            {
                head.localRotation = sixDOFBodyRotation;
            }

            // Draw debug ray in look direction
            Debug.DrawRay(cameraRig.transform.position, lookAt, Color.green);

            // Set pose of the mirrored avatar
            if (showMirror)
            {
                setMirroredAvatarPose();
            }
        }

        /// <summary>
        /// Is triggered when rift orientation changes.
        /// </summary>
        /// <param name="monoscopic">If set to <c>true</c> monoscopic.</param>
        /// <param name="centerEyeAnchor">Center eye anchor.</param>
        /// <param name="leftEyeAnchor">Left eye anchor.</param>
        /// <param name="rightEyeAnchor">Right eye anchor.</param>
        private void riftOrientationChanged(bool monoscopic, Transform centerEyeAnchor, Transform leftEyeAnchor, Transform rightEyeAnchor)
        {
            // Set camera anchor
            this.eyeAnchor = centerEyeAnchor;

            // Avoid clipping of the own body
            //centerEyeAnchor.GetComponent<Camera>().nearClipPlane = 0.01f;

            // Set camera anchor orientations (if internal tracking is activated)
            if (useInternalTracking)
            {
                centerEyeAnchor.localRotation = UVR.InputTracking.GetLocalRotation(UVR.VRNode.CenterEye);
                leftEyeAnchor.localRotation = monoscopic ? centerEyeAnchor.localRotation : UVR.InputTracking.GetLocalRotation(UVR.VRNode.LeftEye);
                rightEyeAnchor.localRotation = monoscopic ? centerEyeAnchor.localRotation : UVR.InputTracking.GetLocalRotation(UVR.VRNode.RightEye);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void setMirroredAvatarPose()
        {
            // Set position of mirrored avatar with 10 units
            Vector3 position = characterJoints.pelvis.position;
            //position.z = -position.z + 5.0f;
            position.z = 3.0f;
            mirroredCharacterJoints.pelvis.position = position;

            // Mirror the rotations
            foreach (Transform joint in characterJoints)
            {
                if (joint != null && joint != characterJoints.root)
                {
                    string name = "";
                    if (joint.name.Contains("Left"))
                    {
                        name = joint.name.Replace("Left", "Right");
                    }
                    else if (joint.name.Contains("Right"))
                    {
                        name = joint.name.Replace("Right", "Left");
                    }
                    else
                    {
                        name = joint.name;
                    }
                    Transform mirroredJoint = null;
                    Component[] comp = mirroredCharacterJoints.root.GetComponentsInChildren<Component>();
                    foreach (Component c in comp)
                    {
                        if (c.transform.name == name) mirroredJoint = c.transform;
                    }
                    if (mirroredJoint)
                    {
                        Vector3 eulerAngles = joint.localEulerAngles;
                        mirroredJoint.localEulerAngles = new Vector3(eulerAngles.x, -eulerAngles.y, -eulerAngles.z);
                    }
                }
            }

            // Rotate body by 180 degrees
            mirroredCharacterJoints.pelvis.localRotation = characterJoints.pelvis.localRotation;
            mirroredCharacterJoints.pelvis.RotateAround(mirroredCharacterJoints.pelvis.position, Vector3.up, 180);
        }
    }
}
