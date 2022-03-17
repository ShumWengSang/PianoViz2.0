using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace MidiVisualization.Scripts
{
    [CreateAssetMenu(fileName = "KeyboardHighlight/Materials", menuName = "KeyboardHighlight/Materials", order = 0)]
    public class KeyboardHighlightMaterials : ScriptableObject
    {
        public Material black;
        public Material irridescent;
        public Material flash;
        [NonSerialized] public string environmentColorThreasholdID = "_EnvironmentColorThreshold";
        [NonSerialized] public string albedoColorID = "_Color";
        [NonSerialized] public string rimLightColorID = "_RimColor";

        private void OnValidate()
        {
            Assert.IsTrue(irridescent.HasProperty(environmentColorThreasholdID));
            Assert.IsTrue(flash.HasProperty(albedoColorID));
            Assert.IsTrue(flash.HasProperty(rimLightColorID));
        }
    }
}