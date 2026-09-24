using UnityEngine;

namespace RestaurantCity {
    // Keep the depth-tested text material attached to Unity's dynamic font atlas.
    public class WorldTextFont : MonoBehaviour {
        public Font Font;
        public Material Material;
        void OnEnable() { UnityEngine.Font.textureRebuilt += Refresh; Refresh(Font); }
        void OnDisable() { UnityEngine.Font.textureRebuilt -= Refresh; }
        void Refresh(Font updated) {
            if (updated == Font && Material && Font && Font.material) Material.mainTexture = Font.material.mainTexture;
        }
    }
}
