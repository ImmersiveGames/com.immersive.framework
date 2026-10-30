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
        public void GroupId_UsesItsOwnDomainAndRejectsInvalidValues()
        {
            GroupId groupId = GroupId.From("  collective.alpha  ");
            GroupId sameGroupId = GroupId.From("collective.alpha");

            Assert.That(groupId.IsValid, Is.True);
            Assert.That(groupId.Domain, Is.EqualTo(FrameworkIdentityDomain.Group));
            Assert.That(groupId, Is.EqualTo(sameGroupId));
            Assert.That(groupId.GetHashCode(), Is.EqualTo(sameGroupId.GetHashCode()));
            Assert.That(groupId.StableText, Is.EqualTo("Group:collective.alpha"));
            Assert.That(default(GroupId).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => new GroupId(default(FrameworkIdentityValue)));
            Assert.Throws<ArgumentException>(() => GroupId.From(" \t "));
        }

        [Test]
        public void GroupProfile_IsTheSingleGroupAuthoringAssetAndRequiresGroupId()
        {
            GroupProfile profile = CreateAsset<GroupProfile>("GroupProfile.asset");

            Assert.That(profile.TryValidate(out string missingIdIssue), Is.False);
            StringAssert.Contains("GroupId", missingIdIssue);

            SetString(profile, "groupId", " \t ");
            Assert.That(profile.TryValidate(out string invalidIdIssue), Is.False);
            StringAssert.Contains("GroupId", invalidIdIssue);

            SetString(profile, "groupId", "group.alpha");
            SetString(profile, "displayName", "Alpha Collective");
            SetString(profile, "description", "Shared presentation data.");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(profile));

            GroupProfile loaded = AssetDatabase.LoadAssetAtPath<GroupProfile>(AssetDatabase.GetAssetPath(profile));
            Assert.That(loaded.TryValidate(out string issue), Is.True, issue);
            Assert.That(loaded.GroupId, Is.EqualTo(GroupId.From("group.alpha")));
            Assert.That(loaded.DisplayName, Is.EqualTo("Alpha Collective"));
            Assert.That(loaded.Description, Is.EqualTo("Shared presentation data."));
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
    }
}
