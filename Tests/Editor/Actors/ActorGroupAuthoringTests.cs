using System;
using Immersive.Framework.Identity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Actors.Tests
{
    public sealed class ActorGroupAuthoringTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/__IFADR051ActorGroupAuthoringTests_" + Guid.NewGuid().ToString("N");
            string folderGuid = AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length));
            Assert.That(folderGuid, Is.Not.Empty, $"Could not create temporary test folder '{_folder}'.");
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_folder) && AssetDatabase.IsValidFolder(_folder))
            {
                Assert.That(AssetDatabase.DeleteAsset(_folder), Is.True);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void GroupAndProfileIdentities_UseDistinctDomainsAndRejectInvalidValues()
        {
            GroupId groupId = GroupId.From("  collective.alpha  ");
            GroupId sameGroupId = GroupId.From("collective.alpha");
            GroupProfileId profileId = GroupProfileId.From("collective.alpha");

            Assert.That(groupId.IsValid, Is.True);
            Assert.That(groupId.Domain, Is.EqualTo(FrameworkIdentityDomain.Group));
            Assert.That(groupId, Is.EqualTo(sameGroupId));
            Assert.That(groupId.GetHashCode(), Is.EqualTo(sameGroupId.GetHashCode()));
            Assert.That(groupId.StableText, Is.EqualTo("Group:collective.alpha"));
            Assert.That(profileId.Domain, Is.EqualTo(FrameworkIdentityDomain.GroupProfile));
            Assert.That(profileId.StableText, Is.EqualTo("GroupProfile:collective.alpha"));
            Assert.That(groupId.Key, Is.Not.EqualTo(profileId.Key));
            Assert.That(default(GroupId).IsValid, Is.False);
            Assert.That(default(GroupProfileId).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new GroupId(default(FrameworkIdentityValue)));
            Assert.Throws<ArgumentException>(() => new GroupProfileId(default(FrameworkIdentityValue)));
            Assert.Throws<ArgumentException>(() => GroupId.From(" \t "));
            Assert.Throws<ArgumentException>(() => GroupProfileId.From(""));
        }

        [Test]
        public void Definition_RequiresExplicitIdentityAndAllowsAnOptionalMissingProfile()
        {
            GroupDefinitionAsset definition = CreateAsset<GroupDefinitionAsset>("GroupDefinition.asset");

            Assert.That(definition.TryValidate(out string missingIdIssue), Is.False);
            StringAssert.Contains("GroupId", missingIdIssue);
            Assert.That(definition.GroupProfile, Is.Null);

            SetString(definition, "groupId", " \t ");
            Assert.That(definition.TryValidate(out string invalidIdIssue), Is.False);
            StringAssert.Contains("GroupId", invalidIdIssue);

            SetString(definition, "groupId", "group.alpha");
            AssetDatabase.SaveAssets();

            Assert.That(definition.TryValidate(out string issue), Is.True, issue);
            Assert.That(definition.GroupId, Is.EqualTo(GroupId.From("group.alpha")));
            Assert.That(definition.GroupProfile, Is.Null);
        }

        [Test]
        public void Definition_RejectsAnAssignedProfileWithInvalidIdentity()
        {
            GroupProfile profile = CreateAsset<GroupProfile>("InvalidProfile.asset");
            GroupDefinitionAsset definition = CreateAsset<GroupDefinitionAsset>("GroupDefinition.asset");
            SetString(definition, "groupId", "group.alpha");
            SetObjectReference(definition, "groupProfile", profile);
            AssetDatabase.SaveAssets();

            Assert.That(definition.TryValidate(out string issue), Is.False);
            StringAssert.Contains("GroupProfile", issue);
        }

        [Test]
        public void GroupProfile_RequiresAnExplicitIdentity()
        {
            GroupProfile profile = CreateAsset<GroupProfile>("GroupProfile.asset");

            Assert.That(profile.TryGetGroupProfileId(out _, out string missingIdIssue), Is.False);
            StringAssert.Contains("GroupProfileId", missingIdIssue);

            SetString(profile, "groupProfileId", " \t ");
            Assert.That(profile.TryGetGroupProfileId(out _, out string invalidIdIssue), Is.False);
            StringAssert.Contains("GroupProfileId", invalidIdIssue);

            SetString(profile, "groupProfileId", "profile.shared");
            AssetDatabase.SaveAssets();

            Assert.That(profile.TryGetGroupProfileId(out GroupProfileId profileId, out string issue), Is.True, issue);
            Assert.That(profileId, Is.EqualTo(GroupProfileId.From("profile.shared")));
        }

        [Test]
        public void DefinitionsCanShareOneProfileWhileKeepingDistinctGroupIdentities()
        {
            GroupProfile profile = CreateAsset<GroupProfile>("SharedProfile.asset");
            SetString(profile, "groupProfileId", "profile.shared");
            SetString(profile, "displayName", "Shared presentation");
            GroupDefinitionAsset first = CreateAsset<GroupDefinitionAsset>("FirstGroup.asset");
            GroupDefinitionAsset second = CreateAsset<GroupDefinitionAsset>("SecondGroup.asset");
            SetString(first, "groupId", "group.first");
            SetObjectReference(first, "groupProfile", profile);
            SetString(second, "groupId", "group.second");
            SetObjectReference(second, "groupProfile", profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(profile));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(first));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(second));

            GroupProfile loadedProfile = AssetDatabase.LoadAssetAtPath<GroupProfile>(
                AssetDatabase.GetAssetPath(profile));
            GroupDefinitionAsset loadedFirst = AssetDatabase.LoadAssetAtPath<GroupDefinitionAsset>(
                AssetDatabase.GetAssetPath(first));
            GroupDefinitionAsset loadedSecond = AssetDatabase.LoadAssetAtPath<GroupDefinitionAsset>(
                AssetDatabase.GetAssetPath(second));

            Assert.That(loadedFirst.TryValidate(out string firstIssue), Is.True, firstIssue);
            Assert.That(loadedSecond.TryValidate(out string secondIssue), Is.True, secondIssue);
            Assert.That(loadedFirst.GroupId, Is.EqualTo(GroupId.From("group.first")));
            Assert.That(loadedSecond.GroupId, Is.EqualTo(GroupId.From("group.second")));
            Assert.That(loadedFirst.GroupId, Is.Not.EqualTo(loadedSecond.GroupId));
            Assert.That(loadedFirst.GroupProfile, Is.SameAs(loadedProfile));
            Assert.That(loadedSecond.GroupProfile, Is.SameAs(loadedProfile));
            Assert.That(loadedProfile.GroupProfileId, Is.EqualTo(GroupProfileId.From("profile.shared")));
            Assert.That(loadedProfile.DisplayName, Is.EqualTo("Shared presentation"));
            Assert.That(typeof(GroupDefinitionAsset).IsSubclassOf(typeof(ScriptableObject)), Is.True);
            Assert.That(typeof(GroupProfile).IsSubclassOf(typeof(ScriptableObject)), Is.True);
        }

        private T CreateAsset<T>(string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, _folder + "/" + fileName);
            return asset;
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
