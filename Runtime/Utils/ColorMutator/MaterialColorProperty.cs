using UnityEngine;

namespace RenderDream.GameEssentials
{
    public class MaterialColorProperty
    {
        public Material material;
        public string name;

        public ColorMutator mutator;
        public float defaultIntensity;

        public float Intensity
        {
            get
            {
                return mutator.exposureValue;
            }
            set
            {
                mutator.exposureValue = value;
                material.SetColor(name, mutator.exposureAdjustedColor);
            }
        }

        public MaterialColorProperty(Material material, string name)
        {
            this.material = material;
            this.name = name;

            mutator = new ColorMutator(material.GetColor(name));
            defaultIntensity = mutator.exposureValue;
        }
    }
}
