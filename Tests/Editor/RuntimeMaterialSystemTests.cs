using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace KrasCore.Tests.Editor
{
    public class RuntimeMaterialTests
    {
        private World _world;
        private RuntimeMaterialSystem _system;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World(nameof(RuntimeMaterialTests), WorldFlags.Editor);
            _system = _world.GetOrCreateSystemManaged<RuntimeMaterialSystem>();
            _entityManager = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }

            _world = null;
            _system = null;
        }

        [Test]
        public void Update_AssignsMaterialFromTextureAndDisablesLookup()
        {
            var srcMaterial = CreateMaterial();
            var texture = CreateTexture(Color.red);

            try
            {
                var entity = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: true, isPrefab: false);

                _system.Update();

                var runtimeMaterial = _entityManager.GetComponentData<RuntimeMaterial>(entity);
                Assert.That(runtimeMaterial.Value.IsValid(), Is.True);

                var assigned = runtimeMaterial.Value.Value;
                Assert.That(assigned, Is.Not.Null);
                Assert.That(ReferenceEquals(assigned, srcMaterial), Is.False);
                Assert.That(assigned.mainTexture, Is.EqualTo(texture));
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(entity), Is.False);
            }
            finally
            {
                DestroyObject(texture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void Update_AssignsSecondarySpriteTextures_WhenLookupUsesSprite()
        {
            var srcMaterial = CreateMaterialWithSecondaryTextureProperty(out var secondaryTextureProperty);
            var mainTexture = CreateTexture(Color.gray, "RuntimeMaterialTests_MainTexture");
            var secondaryTexture = CreateTexture(Color.black, "RuntimeMaterialTests_SecondaryTexture");
            var sprite = CreateSprite(mainTexture, secondaryTextureProperty, secondaryTexture);

            try
            {
                var entity = CreateRuntimeMaterialEntity(srcMaterial, sprite, isEnabled: true, isPrefab: false);

                _system.Update();

                var runtimeMaterial = _entityManager.GetComponentData<RuntimeMaterial>(entity);
                Assert.That(runtimeMaterial.Value.IsValid(), Is.True);

                var assigned = runtimeMaterial.Value.Value;
                Assert.That(assigned, Is.Not.Null);
                Assert.That(assigned.mainTexture, Is.EqualTo(mainTexture));
                Assert.That(assigned.GetTexture(secondaryTextureProperty), Is.EqualTo(secondaryTexture));
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(entity), Is.False);
            }
            finally
            {
                DestroyObject(sprite);
                DestroyObject(secondaryTexture);
                DestroyObject(mainTexture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void Update_ProcessesPrefabEntities()
        {
            var srcMaterial = CreateMaterial();
            var texture = CreateTexture(Color.green);

            try
            {
                var prefabEntity = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: true, isPrefab: true);

                _system.Update();

                var runtimeMaterial = _entityManager.GetComponentData<RuntimeMaterial>(prefabEntity);
                Assert.That(runtimeMaterial.Value.IsValid(), Is.True);
                Assert.That(runtimeMaterial.Value.Value.mainTexture, Is.EqualTo(texture));
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(prefabEntity), Is.False);
            }
            finally
            {
                DestroyObject(texture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void Update_SkipsDisabledLookupUntilEnabled()
        {
            var srcMaterial = CreateMaterial();
            var texture = CreateTexture(Color.blue);

            try
            {
                var entity = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: false, isPrefab: false);

                _system.Update();

                var beforeEnable = _entityManager.GetComponentData<RuntimeMaterial>(entity);
                Assert.That(beforeEnable.Value.IsValid(), Is.False);

                _entityManager.SetComponentEnabled<RuntimeMaterialLookup>(entity, true);
                _system.Update();

                var afterEnable = _entityManager.GetComponentData<RuntimeMaterial>(entity);
                Assert.That(afterEnable.Value.IsValid(), Is.True);
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(entity), Is.False);
            }
            finally
            {
                DestroyObject(texture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void Update_ReusesCachedMaterialForIdenticalTextureLookup()
        {
            var srcMaterial = CreateMaterial();
            var texture = CreateTexture(Color.yellow);

            try
            {
                var first = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: true, isPrefab: false);
                var second = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: true, isPrefab: false);

                _system.Update();

                var firstMaterial = _entityManager.GetComponentData<RuntimeMaterial>(first).Value.Value;
                var secondMaterial = _entityManager.GetComponentData<RuntimeMaterial>(second).Value.Value;

                Assert.That(firstMaterial, Is.Not.Null);
                Assert.That(secondMaterial, Is.Not.Null);
                Assert.That(ReferenceEquals(firstMaterial, secondMaterial), Is.True);
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(first), Is.False);
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(second), Is.False);
            }
            finally
            {
                DestroyObject(texture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void Update_CreatesNewMaterialWhenLookupChangesAfterReenable()
        {
            var srcMaterial = CreateMaterial();
            var firstTexture = CreateTexture(Color.cyan);
            var secondTexture = CreateTexture(Color.magenta);

            try
            {
                var entity = CreateRuntimeMaterialEntity(srcMaterial, firstTexture, isEnabled: true, isPrefab: false);

                _system.Update();
                var firstAssigned = _entityManager.GetComponentData<RuntimeMaterial>(entity).Value.Value;
                Assert.That(firstAssigned.mainTexture, Is.EqualTo(firstTexture));

                _entityManager.SetComponentData(entity, new RuntimeMaterialLookup(srcMaterial, secondTexture));
                _entityManager.SetComponentEnabled<RuntimeMaterialLookup>(entity, true);
                _system.Update();

                var secondAssigned = _entityManager.GetComponentData<RuntimeMaterial>(entity).Value.Value;
                Assert.That(secondAssigned.mainTexture, Is.EqualTo(secondTexture));
                Assert.That(ReferenceEquals(firstAssigned, secondAssigned), Is.False);
                Assert.That(_entityManager.IsComponentEnabled<RuntimeMaterialLookup>(entity), Is.False);
            }
            finally
            {
                DestroyObject(secondTexture);
                DestroyObject(firstTexture);
                DestroyObject(srcMaterial);
            }
        }

        [Test]
        public void OnDestroy_DestroysGeneratedRuntimeMaterials()
        {
            var srcMaterial = CreateMaterial();
            var texture = CreateTexture(Color.white);

            try
            {
                var entity = CreateRuntimeMaterialEntity(srcMaterial, texture, isEnabled: true, isPrefab: false);
                _system.Update();

                var assigned = _entityManager.GetComponentData<RuntimeMaterial>(entity).Value.Value;
                Assert.That(assigned, Is.Not.Null);

                _world.Dispose();
                _world = null;
                _system = null;

                Assert.That(assigned == null, Is.True);
            }
            finally
            {
                DestroyObject(texture);
                DestroyObject(srcMaterial);
            }
        }

        private Entity CreateRuntimeMaterialEntity(Material srcMaterial, Texture texture, bool isEnabled, bool isPrefab)
        {
            var entity = _entityManager.CreateEntity(typeof(RuntimeMaterial), typeof(RuntimeMaterialLookup));
            _entityManager.SetComponentData(entity, new RuntimeMaterialLookup(srcMaterial, texture));
            _entityManager.SetComponentEnabled<RuntimeMaterialLookup>(entity, isEnabled);

            if (isPrefab)
            {
                _entityManager.AddComponent<Prefab>(entity);
            }

            return entity;
        }

        private Entity CreateRuntimeMaterialEntity(Material srcMaterial, Sprite sprite, bool isEnabled, bool isPrefab)
        {
            var entity = _entityManager.CreateEntity(typeof(RuntimeMaterial), typeof(RuntimeMaterialLookup));
            _entityManager.SetComponentData(entity, new RuntimeMaterialLookup(srcMaterial, sprite));
            _entityManager.SetComponentEnabled<RuntimeMaterialLookup>(entity, isEnabled);

            if (isPrefab)
            {
                _entityManager.AddComponent<Prefab>(entity);
            }

            return entity;
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Hidden/InternalErrorShader");

            Assert.That(shader, Is.Not.Null, "No suitable shader was found for RuntimeMaterialSystem tests.");

            var material = new Material(shader)
            {
                name = "RuntimeMaterialTests_SrcMaterial"
            };

            return material;
        }

        private static Material CreateMaterialWithSecondaryTextureProperty(out string secondaryTextureProperty)
        {
            var shaderNames = new[]
            {
                "Standard",
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit"
            };
            var textureProperties = new[]
            {
                "_BumpMap",
                "_MetallicGlossMap",
                "_OcclusionMap",
                "_EmissionMap",
                "_DetailMask",
                "_SpecGlossMap"
            };

            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    continue;
                }

                var material = new Material(shader)
                {
                    name = "RuntimeMaterialTests_SrcMaterial_WithSecondaryTexture"
                };

                foreach (var property in textureProperties)
                {
                    if (!material.HasTexture(property))
                    {
                        continue;
                    }

                    secondaryTextureProperty = property;
                    return material;
                }

                DestroyObject(material);
            }

            Assert.Fail("No suitable shader with a secondary texture property was found for RuntimeMaterialSystem tests.");
            secondaryTextureProperty = null;
            return null;
        }

        private static Sprite CreateSprite(Texture2D mainTexture, string secondaryTextureProperty, Texture2D secondaryTexture)
        {
            var sprite = Sprite.Create(
                mainTexture,
                new Rect(0f, 0f, mainTexture.width, mainTexture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                Vector4.zero,
                false,
                new[]
                {
                    new SecondarySpriteTexture
                    {
                        name = secondaryTextureProperty,
                        texture = secondaryTexture
                    }
                });

            sprite.name = "RuntimeMaterialTests_Sprite";
            return sprite;
        }

        private static Texture2D CreateTexture(Color color, string name = "RuntimeMaterialTests_Texture")
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name
            };

            texture.SetPixel(0, 0, color);
            texture.SetPixel(1, 0, color);
            texture.SetPixel(0, 1, color);
            texture.SetPixel(1, 1, color);
            texture.Apply();

            return texture;
        }

        private static void DestroyObject(Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }
    }
}
