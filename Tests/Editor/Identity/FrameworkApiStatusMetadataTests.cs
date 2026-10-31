using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerSlots;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Immersive.Framework.Identity.Tests
{
    public sealed class FrameworkApiStatusMetadataTests
    {
        [Test]
        public void FrameworkApiStatus_ContainsExactlyTheGovernedCategoriesAndValues()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    "Stable",
                    "Experimental",
                    "Internal",
                    "Deferred",
                    "DevelopmentTooling",
                    "Removed"
                },
                Enum.GetNames(typeof(FrameworkApiStatus)));
            CollectionAssert.AreEqual(
                new[]
                {
                    FrameworkApiStatus.Stable,
                    FrameworkApiStatus.Experimental,
                    FrameworkApiStatus.Internal,
                    FrameworkApiStatus.Deferred,
                    FrameworkApiStatus.DevelopmentTooling,
                    FrameworkApiStatus.Removed
                },
                (FrameworkApiStatus[])Enum.GetValues(typeof(FrameworkApiStatus)));
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 3, 4, 5 },
                Array.ConvertAll(
                    (FrameworkApiStatus[])Enum.GetValues(typeof(FrameworkApiStatus)),
                    status => (int)status));
        }

        [Test]
        public void FrameworkApiStatusAttribute_HasStableNonInheritedSingleUseTypeMetadata()
        {
            AttributeUsageAttribute usage = typeof(FrameworkApiStatusAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            Assert.That(usage, Is.Not.Null);
            Assert.That(
                usage.ValidOn,
                Is.EqualTo(
                    AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Interface |
                    AttributeTargets.Enum |
                    AttributeTargets.Delegate));
            Assert.That(usage.AllowMultiple, Is.False);
            Assert.That(usage.Inherited, Is.False);
            AssertStable(typeof(FrameworkApiStatusAttribute));
        }

        [Test]
        public void FrameworkApiStatusAttribute_StoresReadonlyNormalizedMetadata()
        {
            var metadata = new FrameworkApiStatusAttribute(
                FrameworkApiStatus.Experimental,
                "  controlled development surface  ");

            Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental));
            Assert.That(metadata.Note, Is.EqualTo("controlled development surface"));
            Assert.That(typeof(FrameworkApiStatusAttribute).GetProperty(nameof(metadata.Status)).SetMethod, Is.Null);
            Assert.That(typeof(FrameworkApiStatusAttribute).GetProperty(nameof(metadata.Note)).SetMethod, Is.Null);
        }

        [Test]
        public void IdentityAndMaturityGovernanceContracts_AreStableAndListedInPublicApi()
        {
            Type[] stableContracts =
            {
                typeof(IFrameworkIdentity),
                typeof(FrameworkIdentityDomain),
                typeof(FrameworkIdentityKey),
                typeof(FrameworkIdentityValue),
                typeof(RouteId),
                typeof(ActivityId),
                typeof(PlayerSlotId),
                typeof(FrameworkApiStatus),
                typeof(FrameworkApiStatusAttribute)
            };

            foreach (Type contract in stableContracts)
            {
                AssertStable(contract);
            }

            string packageRoot = PackageInfo.FindForAssembly(typeof(FrameworkIdentityKey).Assembly).resolvedPath;
            string publicApi = File.ReadAllText(Path.Combine(packageRoot, "Documentation~", "API", "Public-API.md"));
            string governance = File.ReadAllText(Path.Combine(
                packageRoot,
                "Documentation~",
                "Architecture",
                "Governance",
                "IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md"));

            string identityRow = publicApi
                .Replace("\r", string.Empty)
                .Split('\n')
                .Single(line => line.Contains("`IFrameworkIdentity`"));
            StringAssert.Contains("Stable", identityRow);
            foreach (Type contract in stableContracts.Take(7))
            {
                StringAssert.Contains("`" + contract.Name + "`", identityRow);
            }

            string[] governedNames =
            {
                "Stable",
                "Experimental",
                "Internal",
                "Deferred",
                "DevelopmentTooling",
                "Removed"
            };
            foreach (string name in governedNames)
            {
                StringAssert.Contains("| `" + name + "` |", governance);
            }
        }

        [Test]
        public void FrameworkApiStatusInventory_RejectsUndefinedStatusValues()
        {
            var invalidDeclarations = new List<string>();
            int scannedAssemblyCount = 0;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                if (string.IsNullOrEmpty(assemblyName) ||
                    !assemblyName.StartsWith("Immersive.Framework.", StringComparison.Ordinal) ||
                    assemblyName.Contains(".Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                scannedAssemblyCount++;

                foreach (Type type in assembly.GetTypes())
                {
                    CustomAttributeData metadata = type.CustomAttributes.FirstOrDefault(attribute =>
                        attribute.AttributeType == typeof(FrameworkApiStatusAttribute));
                    if (metadata == null || metadata.ConstructorArguments.Count == 0)
                    {
                        continue;
                    }

                    FrameworkApiStatus status = (FrameworkApiStatus)Enum.ToObject(
                        typeof(FrameworkApiStatus),
                        metadata.ConstructorArguments[0].Value);
                    if (!Enum.IsDefined(typeof(FrameworkApiStatus), status))
                    {
                        invalidDeclarations.Add(assemblyName + ":" + type.FullName + "=" + (int)status);
                    }
                }
            }

            Assert.That(scannedAssemblyCount, Is.GreaterThan(0), "No Framework production assembly was inventoried.");
            Assert.That(invalidDeclarations, Is.Empty, string.Join(Environment.NewLine, invalidDeclarations));
        }

        [Test]
        public void ActorGroupIdentityAndAuthoringContracts_AreExperimentalAndListedInPublicApi()
        {
            Type[] contracts =
            {
                typeof(GroupId),
                typeof(GroupProfile)
            };

            foreach (Type contract in contracts)
            {
                FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
                Assert.That(metadata, Is.Not.Null, contract.FullName + " is missing its maturity attribute.");
                Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental), contract.FullName);
                Assert.That(metadata.Note, Is.Not.Empty, contract.FullName);
            }

            string packageRoot = PackageInfo.FindForAssembly(typeof(FrameworkIdentityKey).Assembly).resolvedPath;
            string publicApi = File.ReadAllText(Path.Combine(packageRoot, "Documentation~", "API", "Public-API.md"));
            foreach (Type contract in contracts)
            {
                string apiEntry = publicApi
                    .Replace("\r", string.Empty)
                    .Split('\n')
                    .Single(line =>
                    {
                        string[] columns = line.Split('|');
                        return columns.Length > 2 && columns[1].Contains("`" + contract.Name + "`");
                    });
                StringAssert.Contains("Experimental", apiEntry, contract.Name);
            }
        }

        [Test]
        public void ActorOccurrenceQueryContracts_AreExperimentalAndListedInPublicApi()
        {
            Type[] contracts =
            {
                typeof(IActorOccurrenceQuery),
                typeof(ActorOccurrenceQueryBinding)
            };

            foreach (Type contract in contracts)
            {
                FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
                Assert.That(metadata, Is.Not.Null, contract.FullName + " is missing its maturity attribute.");
                Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Experimental), contract.FullName);
                Assert.That(metadata.Note, Is.Not.Empty, contract.FullName);
            }

            string packageRoot = PackageInfo.FindForAssembly(typeof(FrameworkIdentityKey).Assembly).resolvedPath;
            string publicApi = File.ReadAllText(Path.Combine(packageRoot, "Documentation~", "API", "Public-API.md"));
            foreach (Type contract in contracts)
            {
                string apiEntry = publicApi
                    .Replace("\r", string.Empty)
                    .Split('\n')
                    .Single(line =>
                    {
                        string[] columns = line.Split('|');
                        return columns.Length > 2 && columns[1].Contains("`" + contract.Name + "`");
                    });
                StringAssert.Contains("Experimental", apiEntry, contract.Name);
            }
        }

        private static void AssertStable(Type contract)
        {
            FrameworkApiStatusAttribute metadata = contract.GetCustomAttribute<FrameworkApiStatusAttribute>(false);
            Assert.That(metadata, Is.Not.Null, contract.FullName + " is missing its maturity attribute.");
            Assert.That(metadata.Status, Is.EqualTo(FrameworkApiStatus.Stable), contract.FullName);
            Assert.That(metadata.Note, Is.Not.Empty, contract.FullName);
        }
    }
}
