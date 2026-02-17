using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace KrasCore.Tests.Editor
{
    public class RuntimeMaterialSystemTests
    {
        private World _world;
        private RuntimeMaterialSystem _system;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World(nameof(RuntimeMaterialSystemTests), WorldFlags.Editor);
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
        public void Update_AssignsMaterialAndDisablesLookup()
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
        public void Update_ReusesCachedMaterialForIdenticalLookup()
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
                name = "RuntimeMaterialSystemTests_SrcMaterial"
            };

            return material;
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "RuntimeMaterialSystemTests_Texture"
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
