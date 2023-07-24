﻿// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable CommentTypo, StringLiteralTypo, IdentifierTypo, InconsistentNaming
namespace ISTA_Patcher;

using System.Runtime.CompilerServices;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Serilog;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

internal static class PatchUtils
{
    private static readonly string Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    private static readonly ModuleContext ModCtx = ModuleDef.CreateModuleContext();
    private static readonly IDeobfuscatorContext DeobfuscatorContext = new DeobfuscatorContext();
    private static readonly NewProcessAssemblyClientFactory ProcessAssemblyClientFactory = new();

    private static string Version
    {
        get
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
            return version.ToString();
        }
    }

    /// <summary>
    /// Loads a module from the specified file.
    /// </summary>
    /// <param name="fileName">The path to the module file.</param>
    /// <returns>The loaded <see cref="ModuleDefMD"/>.</returns>
    public static ModuleDefMD LoadModule(string fileName)
    {
        var module = ModuleDefMD.Load(fileName, ModCtx);
        return module;
    }

    /// <summary>
    /// Applies a patch to a method in the specified assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="AssemblyDefinition"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="desc">The description of the method.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    private static int PatchFunction(
        this AssemblyDefinition assembly,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var function = assembly.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Assembly}: {Result}", memberName, name, assembly, function != null);
        if (function == null)
        {
            return 0;
        }

        operation(function);
        return 1;
    }

    [ValidationPatch]
    public static int PatchLicenseStatusChecker(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
            "IsLicenseValid",
            "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseWizardHelper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
            "DoLicenseCheck",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseManager(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "VerifyLicense",
            "(System.Boolean)System.Void",
            DnlibUtils.EmptyingMethod
        ) + assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "CheckRITALicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        ) + assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseManager",
            "LastCompileTimeIsInvalid",
            "()System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [ValidationPatch]
    public static int PatchLicenseHelper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.LicenseHelper",
            "IsVehicleLockedDown",
            "(BMW.Rheingold.CoreFramework.DatabaseProvider.Vehicle)System.Boolean",
            DnlibUtils.ReturnFalseMethod
        );
    }

    [ValidationPatch]
    public static int PatchCommonServiceWrapper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
            "VerifyLicense",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    public static int PatchSecureAccessHelper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
            "IsCodeAccessPermitted",
            "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchFscValidationClient(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.TricTools.FscValidation.FscValidationClient",
            "IsValid",
            "(System.Byte[],System.Byte[])System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ValidationPatch]
    public static int PatchMainWindowViewModel(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
            "CheckExpirationDate",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [ValidationPatch]
    public static int PatchActivationCertificateHelper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsInWhiteList",
            "(System.String,System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        ) + assembly.PatchFunction(
            "BMW.iLean.CommonServices.Helper.ActivationCertificateHelper",
            "IsWhiteListSignatureValid",
            "(System.String,System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ENETPatch]
    public static int PatchTherapyPlanCalculated(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.Programming.States.TherapyPlanCalculated",
            "IsConnectedViaENETAndBrandIsToyota",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
         );
    }

    [RequirementsPatch]
    public static int PatchIstaInstallationRequirements(AssemblyDefinition assembly)
    {
        void RemoveRequirementsCheck(MethodDef method)
        {
            var dictionaryCtorRef = method.FindOperand<MemberRef>(
                OpCodes.Newobj,
                "System.Void System.Collections.Generic.Dictionary`2<BMW.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>::.ctor()");

            if (dictionaryCtorRef == null)
            {
                Log.Warning("Required instructions not found, can not patch IstaInstallationRequirements::CheckSystemRequirements");
                return;
            }

            method.ReturnObjectMethod(dictionaryCtorRef);
        }

        return assembly.PatchFunction(
            "BMW.Rheingold.ISTAGUI.Controller.IstaInstallationRequirements",
            "CheckSystemRequirements",
            "(System.Boolean)System.Collections.Generic.Dictionary`2<BMW.Rheingold.ISTAGUI._new.ViewModels.InsufficientSystemRequirement,System.Int32[]>",
            RemoveRequirementsCheck
        );
    }

    [NotSendPatch]
    public static int PatchMultisessionLogic(AssemblyDef assembly)
    {
        void SetNotSendOBFCMData(MethodDef method)
        {
            var get_CurrentOperation = method.FindOperand<MethodDef>(OpCodes.Call, "BMW.Rheingold.PresentationFramework.Contracts.IIstaOperation BMW.Rheingold.ISTAGUI.Controller.MultisessionLogic::get_CurrentOperation()");
            var setIsSendOBFCMDataIsForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void BMW.ISPI.IstaOperation.Contract.IIstaOperationService::SetIsSendOBFCMDataIsForbidden(System.Boolean)");
            var onPropertyChanged = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void BMW.Rheingold.RheingoldSessionController.Logic::OnPropertyChanged(System.String)");

            if (get_CurrentOperation == null || setIsSendOBFCMDataIsForbidden == null || onPropertyChanged == null)
            {
                Log.Warning("Required instructions not found, can not patch MultisessionLogic::SetNotSendOBFCMData");
                return;
            }

            var patchedMethod = new[]
            {
                // this.CurrentOperation.SetIsSendOBFCMDataIsForbidden(true);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setIsSendOBFCMDataIsForbidden),

                // this.OnPropertyChanged("isSendOBFCMDataForbidden");
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("isSendOBFCMDataForbidden"),
                OpCodes.Callvirt.ToInstruction(onPropertyChanged),

                // return;
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }

        void SetNotSendFastData(MethodDef method)
        {
            var get_CurrentOperation = method.FindOperand<MethodDef>(OpCodes.Call, "BMW.Rheingold.PresentationFramework.Contracts.IIstaOperation BMW.Rheingold.ISTAGUI.Controller.MultisessionLogic::get_CurrentOperation()");
            var get_DataContext = method.FindOperand<MemberRef>(OpCodes.Callvirt, "BMW.ISPI.IstaOperation.Contract.IIstaOperationDataContext BMW.Rheingold.PresentationFramework.Contracts.IIstaOperation::get_DataContext()");
            var get_VecInfo = method.FindOperand<MemberRef>(OpCodes.Callvirt, "BMW.Rheingold.CoreFramework.Contracts.Vehicle.IVehicle BMW.ISPI.IstaOperation.Contract.IIstaOperationDataContext::get_VecInfo()");
            var set_IsSendFastaDataForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void BMW.Rheingold.CoreFramework.Contracts.Vehicle.IVehicle::set_IsSendFastaDataForbidden(System.Boolean)");
            var setIsSendFastaDataIsForbidden = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void BMW.ISPI.IstaOperation.Contract.IIstaOperationService::SetIsSendFastaDataIsForbidden(System.Boolean)");
            var onPropertyChanged = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void BMW.Rheingold.RheingoldSessionController.Logic::OnPropertyChanged(System.String)");

            if (get_CurrentOperation == null || get_DataContext == null || get_VecInfo == null || set_IsSendFastaDataForbidden == null || setIsSendFastaDataIsForbidden == null || onPropertyChanged == null)
            {
                Log.Warning("Required instructions not found, can not patch MultisessionLogic::SetNotSendFastData");
                return;
            }

            var patchedMethod = new[]
            {
                // this.CurrentOperation.DataContext.VecInfo.IsSendFastaDataForbidden = true;
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Callvirt.ToInstruction(get_DataContext),
                OpCodes.Callvirt.ToInstruction(get_VecInfo),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(set_IsSendFastaDataForbidden),

                // this.CurrentOperation.SetIsSendFastaDataIsForbidden(true);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(get_CurrentOperation),
                OpCodes.Ldc_I4_1.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setIsSendFastaDataIsForbidden),

                // this.OnPropertyChanged("IsSendFastaDataForbidden");
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("IsSendFastaDataForbidden"),
                OpCodes.Callvirt.ToInstruction(onPropertyChanged),

                // return;
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }

        return assembly.PatchFunction(
            "BMW.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "SetIsSendFastaDataForbidden",
            "()System.Void",
            SetNotSendFastData
        ) + assembly.PatchFunction(
            "BMW.Rheingold.ISTAGUI.Controller.MultisessionLogic",
            "SetIsSendOBFCMDataForbidden",
            "()System.Void",
            SetNotSendOBFCMData
        );
    }

    [ToyotaPatch]
    public static int PatchCommonFuncForIsta(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "Toyota.GTS.ForIsta.CommonFuncForIsta",
            "GetLicenseStatus",
            "()BMW.Rheingold.ToyotaLicenseHelper.ToyotaLicenseStatus",
            DnlibUtils.ReturnZeroMethod
        );
    }

    [ToyotaPatch]
    public static int PatchToyotaWorker(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.Toyota.Worker.ToyotaWorker",
            "VehicleIsValid",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [ToyotaPatch]
    public static int PatchIndustrialCustomerManager(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.IndustrialCustomer.Manager.IndustrialCustomerManager",
            "IsIndustrialCustomerBrand",
            "(System.String)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchIntegrityManager(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
            ".ctor",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchVerifyAssemblyHelper(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
            "VerifyStrongName",
            "(System.String,System.Boolean)System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchIstaIcsServiceClient(AssemblyDefinition assembly)
    {
        void RemovePublicKeyCheck(MethodDef method)
        {
            var getProcessesByName = DnlibUtils.BuildCall(assembly.ManifestModule, typeof(System.Diagnostics.Process), "GetProcessesByName", typeof(System.Diagnostics.Process[]), new[] { typeof(string) });
            var firstOrDefault = method.FindOperand<MethodSpec>(OpCodes.Call, "System.Diagnostics.Process System.Linq.Enumerable::FirstOrDefault<System.Diagnostics.Process>(System.Collections.Generic.IEnumerable`1<System.Diagnostics.Process>)");
            var invalidOperationException = DnlibUtils.BuildCall(assembly.ManifestModule, typeof(InvalidOperationException), ".ctor", typeof(void), new[] { typeof(string) });
            if (getProcessesByName == null || firstOrDefault == null || invalidOperationException == null)
            {
                Log.Warning("Required instructions not found, can not patch IstaIcsServiceClient::ValidateHost");
                return;
            }

            var ret = OpCodes.Ret.ToInstruction();
            var patchedMethod = new[]
            {
                // if (Process.GetProcessesByName("IstaServicesHost").FirstOrDefault() == null)
                OpCodes.Ldstr.ToInstruction("IstaServicesHost"),
                OpCodes.Call.ToInstruction(getProcessesByName),
                OpCodes.Call.ToInstruction(firstOrDefault),
                OpCodes.Brtrue_S.ToInstruction(ret),

                // throw new InvalidOperationException("Host not found.");
                OpCodes.Ldstr.ToInstruction("Host not found."),
                OpCodes.Newobj.ToInstruction(invalidOperationException),
                OpCodes.Throw.ToInstruction(),

                ret,
            };

            method.ReplaceWith(patchedMethod);
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
        }

        return assembly.PatchFunction(
            "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
            "ValidateHost",
            "()System.Void",
            RemovePublicKeyCheck
        );
    }

    [EssentialPatch]
    public static int PatchIstaProcessStarter(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
            "CheckSignature",
            "(System.String)System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchPackageValidityService(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.ISTAGUI.Controller.PackageValidityService",
            "CyclicExpirationDateCheck",
            "()System.Void",
            DnlibUtils.EmptyingMethod
        );
    }

    [EssentialPatch]
    public static int PatchServiceProgramCompilerLicense(AssemblyDefinition assembly)
    {
        return assembly.PatchFunction(
            "BMW.Rheingold.ExternalToolLicense.ServiceProgramCompilerLicense",
            "CheckLicenseExpiration",
            "()System.Boolean",
            DnlibUtils.ReturnTrueMethod
        );
    }

    [EssentialPatch]
    public static int PatchConfigurationService(AssemblyDefinition assembly)
    {
        void RewriteProperties(MethodDef method)
        {
            var getBaseService = method.FindOperand<MemberRef>(OpCodes.Call, "com.bmw.psdz.api.Configuration BMW.Rheingold.Psdz.Services.ServiceBase`1<com.bmw.psdz.api.Configuration>::get_BaseService()");
            var getPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "java.util.Properties com.bmw.psdz.api.Configuration::getPSdZProperties()");
            var setPSdZProperties = method.FindOperand<MemberRef>(OpCodes.Callvirt, "System.Void com.bmw.psdz.api.Configuration::setPSdZProperties(java.util.Properties)");
            var putProperty = method.FindOperand<MethodDef>(OpCodes.Call, "System.Void BMW.Rheingold.Psdz.Services.ConfigurationService::PutProperty(java.util.Properties,java.lang.String,java.lang.String)");
            var stringImplicit = method.FindOperand<MemberRef>(OpCodes.Call, "java.lang.String java.lang.String::op_Implicit(System.String)");

            if (getBaseService == null || getPSdZProperties == null || setPSdZProperties == null || putProperty == null ||
                stringImplicit == null)
            {
                Log.Warning("Required instructions not found, can not patch ConfigurationService::SetPsdzProperties");
                return;
            }

            var patchedMethod = new[]
            {
                // Properties pSdZProperties = base.BaseService.getPSdZProperties();
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Callvirt.ToInstruction(getPSdZProperties),
                OpCodes.Stloc_0.ToInstruction(),

                // PutProperty(pSdZProperties, String.op_Implicit("DealerID"), String.op_Implicit("1234"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("DealerID"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldstr.ToInstruction("1234"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("PlantID"), String.op_Implicit("0"));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("PlantID"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldstr.ToInstruction("0"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("ProgrammierGeraeteSeriennummer"), String.op_Implicit(programmierGeraeteSeriennummer));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("ProgrammierGeraeteSeriennummer"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldarg_3.ToInstruction(),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // PutProperty(pSdZProperties, String.op_Implicit("Testereinsatzkennung"), String.op_Implicit(testerEinsatzKennung));
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Ldstr.ToInstruction("Testereinsatzkennung"),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Ldarg_S.ToInstruction(method.Parameters[4]),
                OpCodes.Call.ToInstruction(stringImplicit),
                OpCodes.Call.ToInstruction(putProperty),

                // base.BaseService.setPSdZProperties(pSdZProperties);
                OpCodes.Ldarg_0.ToInstruction(),
                OpCodes.Call.ToInstruction(getBaseService),
                OpCodes.Ldloc_0.ToInstruction(),
                OpCodes.Callvirt.ToInstruction(setPSdZProperties),
                OpCodes.Ret.ToInstruction(),
            };

            method.ReplaceWith(patchedMethod);
            var property =
                method.Body.Variables.FirstOrDefault(variable => variable.Type.FullName == "java.util.Properties");
            if (property == null)
            {
                Log.Warning("Properties not found, patch for ConfigurationService may not workable");
                return;
            }

            method.Body.Variables.Clear();
            method.Body.Variables.Add(property);
        }

        return assembly.PatchFunction(
            "BMW.Rheingold.Psdz.Services.ConfigurationService",
            "SetPsdzProperties",
            "(System.String,System.String,System.String,System.String)System.Void",
            RewriteProperties
        );
    }

    [EssentialPatch]
    public static int PatchInteractionModel(AssemblyDefinition assembly)
    {
        void RewriteTitle(MethodDef method)
        {
            var mod = method.Module;
            TypeRef stringRef = new TypeRefUser(mod, "System", "String", mod.CorLibTypes.AssemblyRef);
            MemberRef concatRef = new MemberRefUser(
                mod,
                "Concat",
                MethodSig.CreateStatic(mod.CorLibTypes.String, mod.CorLibTypes.String, mod.CorLibTypes.String),
                stringRef);

            var instructions = method.Body.Instructions;
            instructions.RemoveAt(instructions.Count - 1);
            instructions.Add(OpCodes.Ldstr.ToInstruction($" (Powered by ISTA-Patcher v{Version})"));
            instructions.Add(OpCodes.Call.ToInstruction(concatRef));
            instructions.Add(OpCodes.Ret.ToInstruction());
        }

        return assembly.PatchFunction(
            "BMW.Rheingold.CoreFramework.Interaction.Models.InteractionModel",
            "get_Title",
            "()System.String",
            RewriteTitle
        );
    }

    [SignaturePatch]
    public static Func<AssemblyDefinition, int> PatchGetRSAPKCS1SignatureDeformatter(string modulus, string exponent)
    {
        return assembly =>
        {
            return PatchFunction(
                assembly,
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "GetRSAPKCS1SignatureDeformatter",
                "()System.Security.Cryptography.RSAPKCS1SignatureDeformatter",
                ReplaceParameters
            );

            void ReplaceParameters(MethodDef method)
            {
                var lsStrInstructions = method.Body.Instructions.Where(inst => inst.OpCode == OpCodes.Ldstr).ToList();
                if (lsStrInstructions.Count == 3)
                {
                    lsStrInstructions[0].Operand = modulus;
                    lsStrInstructions[1].Operand = exponent;
                }
                else
                {
                    Log.Warning("instruction ldstr count not match, can not patch LicenseStatusChecker");
                }
            }
        };
    }

    /// <summary>
    /// Check if the assembly is patched by this patcher.
    /// </summary>
    /// <param name="assembly">assembly to check.</param>
    /// <returns>ture for assembly has been patched.</returns>
    public static bool HavePatchedMark(AssemblyDefinition assembly)
    {
        var patchedType = assembly.Modules.First().GetType("Patched.By.TC");
        return patchedType != null;
    }

    /// <summary>
    /// Set the patched mark to the assembly.
    /// </summary>
    /// <param name="assembly">assembly to set.</param>
    public static void SetPatchedMark(AssemblyDefinition assembly)
    {
        var module = assembly.Modules.FirstOrDefault();
        if (module == null || HavePatchedMark(assembly))
        {
            return;
        }

        var patchedType = new TypeDefUser(
            "Patched.By",
            "TC",
            module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.Class | TypeAttributes.NestedPrivate,
        };
        var dateField = new FieldDefUser(
            "date",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Timestamp),
        };
        var urlField = new FieldDefUser(
            "repo",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser("https://github.com/tautcony/ISTA-Patcher"),
        };

        var versionField = new FieldDefUser(
            "version",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Version),
        };

        patchedType.Fields.Add(dateField);
        patchedType.Fields.Add(urlField);
        patchedType.Fields.Add(versionField);
        module.Types.Add(patchedType);
    }

    /// <summary>
    /// Performs deobfuscation on an obfuscated file and saves the result to a new file.
    /// </summary>
    /// <param name="fileName">The path to the obfuscated file.</param>
    /// <param name="newFileName">The path to save the deobfuscated file.</param>
    public static void DeObfuscation(string fileName, string newFileName)
    {
        var deobfuscatorInfo = new DeobfuscatorInfo();

        using var file = new ObfuscatedFile(
            new ObfuscatedFile.Options
        {
            ControlFlowDeobfuscation = true,
            Filename = fileName,
            NewFilename = newFileName,
            StringDecrypterType = DecrypterType.Static,
        },
            ModCtx,
            ProcessAssemblyClientFactory)
        {
            DeobfuscatorContext = DeobfuscatorContext,
        };

        file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
        file.DeobfuscateBegin();
        file.Deobfuscate();
        file.DeobfuscateEnd();
        file.Save();
    }
}
