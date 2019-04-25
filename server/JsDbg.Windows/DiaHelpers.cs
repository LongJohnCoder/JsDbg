﻿//--------------------------------------------------------------
//
//    MIT License
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//
//--------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dia2Lib;
using System.Runtime.InteropServices;

namespace JsDbg.Windows.Dia {
    [ 
         ComImport, 
         Guid("8E3F80CA-7517-432a-BA07-285134AAEA8E"), 
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown), 
         ComVisible(true)
    ] 
    public interface IDiaReadExeAtRVACallback {
        void ReadExecutableAtRVA(uint relativeVirtualAddress, uint cbData, ref uint pcbData, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
    }

    [
         ComImport,
         Guid("C32ADB82-73F4-421b-95D5-A4706EDF5DBE"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
         ComVisible(true)
    ] 
    public interface IDiaLoadCallback {
        void NotifyDebugDir(bool fExecutable, uint cbData, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
        void NotifyOpenDBG([MarshalAs(UnmanagedType.LPWStr)]string dbgPath, uint resultCode);
        void NotifyOpenPDB([MarshalAs(UnmanagedType.LPWStr)]string pdbPath, uint resultCode);
        void RestrictRegistryAccess();
        void RestrictSymbolServerAccess();
    }

    public static class DiaHelpers {
        public enum LocationType {
            LocIsNull = 0,
            LocIsStatic = 1,
            LocIsTLS = 2,
            LocIsRegRel = 3,
            LocIsThisRel = 4,
            LocIsEnregistered = 5,
            LocIsBitField = 6,
            LocIsSlot = 7,
            LocIsIlRel = 8,
            LocInMetaData = 9,
            LocIsConstant = 10,
            LocTypeMax = 10
        }

        public enum BasicType {
            btNoType = 0,
            btVoid = 1,
            btChar = 2,
            btWChar = 3,
            btInt = 6,
            btUInt = 7,
            btFloat = 8,
            btBCD = 9,
            btBool = 10,
            btLong = 13,
            btULong = 14,
            btCurrency = 25,
            btDate = 26,
            btVariant = 27,
            btComplex = 28,
            btBit = 29,
            btBSTR = 30,
            btHresult = 31
        }

        public enum NameSearchOptions {
            nsNone = 0,
            nsfCaseSensitive = 0x1,
            nsfCaseInsensitive = 0x2,
            nsfFNameExt = 0x4,
            nsfRegularExpression = 0x8,
            nsfUndecoratedName = 0x10,

            // For backward compatibility:
            nsCaseSensitive = nsfCaseSensitive,
            nsCaseInsensitive = nsfCaseInsensitive,
            nsFNameExt = nsfCaseInsensitive | nsfFNameExt,
            nsRegularExpression = nsfRegularExpression | nsfCaseSensitive,
            nsCaseInRegularExpression = nsfRegularExpression | nsfCaseInsensitive
        }

        public static string GetBasicTypeName(BasicType type, ulong size) {
            switch (type) {
            case BasicType.btVoid:
                return "void";
            case BasicType.btWChar:
                return "wchar_t";
            case BasicType.btChar:
            case BasicType.btLong:
            case BasicType.btInt:
                if (size <= 1) {
                    return "char";
                } else if (size <= 2) {
                    return "short";
                } else if (size <= 4) {
                    return "int";
                } else if (size <= 8) {
                    return "long long";
                }
                break;
            case BasicType.btULong:
            case BasicType.btUInt:
                return "unsigned " + GetBasicTypeName(BasicType.btInt, size);
            case BasicType.btFloat:
                if (size <= 4) {
                    return "float";
                } else if (size <= 8) {
                    return "double";
                }
                break;
            case BasicType.btBool:
                return "bool";
            case BasicType.btNoType:
            case BasicType.btBCD:
            case BasicType.btCurrency:
            case BasicType.btDate:
            case BasicType.btVariant:
            case BasicType.btComplex:
            case BasicType.btBit:
            case BasicType.btBSTR:
            case BasicType.btHresult:
            default:
                break;
            }

            System.Diagnostics.Debug.WriteLine("Unable to get type name for basic type {0} with size {1}", type, size);
            return "void";
        }

        public static string GetTypeName(IDiaSymbol typeSymbol, int pointerAdjustment = 0) {
            switch ((SymTagEnum)typeSymbol.symTag) {
                case SymTagEnum.SymTagArrayType:
                    return GetTypeName(typeSymbol.type) + "[" + typeSymbol.count + "]";
                case SymTagEnum.SymTagBaseType:
                    return GetBasicTypeName((BasicType)typeSymbol.baseType, typeSymbol.length);
                case SymTagEnum.SymTagPointerType:
                    if (pointerAdjustment != 0) {
                        return String.Format("{0}({2}{1})*", GetTypeName(typeSymbol.type), pointerAdjustment, pointerAdjustment > 0 ? "+" : "");
                    } else {
                        return GetTypeName(typeSymbol.type) + "*";
                    }
                case SymTagEnum.SymTagTypedef:
                case SymTagEnum.SymTagEnum:
                case SymTagEnum.SymTagUDT:
                    return typeSymbol.name;
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine("Unable to get a type name for {0} ({1})", typeSymbol.name, (SymTagEnum)typeSymbol.symTag);
            return "void";
        }

#if DEBUG
        /// <summary>
        /// Gets all the IDiaSymbol properties
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool GetEveryProperty(IDiaSymbol symbol) {
            var access = symbol.access;
            var addressOffset = symbol.addressOffset;
            var addressSection = symbol.addressSection;
            var addressTaken = symbol.addressTaken;
            var age = symbol.age;
            var arrayIndexType = symbol.arrayIndexType;
            var arrayIndexTypeId = symbol.arrayIndexTypeId;
            var backEndBuild = symbol.backEndBuild;
            var backEndMajor = symbol.backEndMajor;
            var backEndMinor = symbol.backEndMinor;
            var backEndQFE = symbol.backEndQFE;
            var baseDataOffset = symbol.baseDataOffset;
            var baseDataSlot = symbol.baseDataSlot;
            var baseSymbol = symbol.baseSymbol;
            var baseSymbolId = symbol.baseSymbolId;
            var baseType = symbol.baseType;
            var bindID = symbol.bindID;
            var bindSlot = symbol.bindSlot;
            var bindSpace = symbol.bindSpace;
            var bitPosition = symbol.bitPosition;
            var builtInKind = symbol.builtInKind;
            var callingConvention = symbol.callingConvention;
            var characteristics = symbol.characteristics;
            var classParent = symbol.classParent;
            var classParentId = symbol.classParentId;
            var code = symbol.code;
            //var coffGroup = symbol.coffGroup;
            var compilerGenerated = symbol.compilerGenerated;
            var compilerName = symbol.compilerName;
            var constantExport = symbol.constantExport;
            var constructor = symbol.constructor;
            var constType = symbol.constType;
            var container = symbol.container;
            var count = symbol.count;
            var countLiveRanges = symbol.countLiveRanges;
            var customCallingConvention = symbol.customCallingConvention;
            var dataExport = symbol.dataExport;
            var dataKind = symbol.dataKind;
            var editAndContinueEnabled = symbol.editAndContinueEnabled;
            var exceptionHandlerAddressOffset = symbol.exceptionHandlerAddressOffset;
            var exceptionHandlerAddressSection = symbol.exceptionHandlerAddressSection;
            var exceptionHandlerRelativeVirtualAddress = symbol.exceptionHandlerRelativeVirtualAddress;
            var exceptionHandlerVirtualAddress = symbol.exceptionHandlerVirtualAddress;
            var exportHasExplicitlyAssignedOrdinal = symbol.exportHasExplicitlyAssignedOrdinal;
            var exportIsForwarder = symbol.exportIsForwarder;
            var farReturn = symbol.farReturn;
            var finalLiveStaticSize = symbol.finalLiveStaticSize;
            var framePointerPresent = symbol.framePointerPresent;
            var frameSize = symbol.frameSize;
            var frontEndBuild = symbol.frontEndBuild;
            var frontEndMajor = symbol.frontEndMajor;
            var frontEndMinor = symbol.frontEndMinor;
            var frontEndQFE = symbol.frontEndQFE;
            var function = symbol.function;
            var guid = symbol.guid;
            var hasAlloca = symbol.hasAlloca;
            var hasAssignmentOperator = symbol.hasAssignmentOperator;
            var hasCastOperator = symbol.hasCastOperator;
            var hasControlFlowCheck = symbol.hasControlFlowCheck;
            var hasDebugInfo = symbol.hasDebugInfo;
            var hasEH = symbol.hasEH;
            var hasEHa = symbol.hasEHa;
            var hasInlAsm = symbol.hasInlAsm;
            var hasLongJump = symbol.hasLongJump;
            var hasManagedCode = symbol.hasManagedCode;
            var hasNestedTypes = symbol.hasNestedTypes;
            var hasSecurityChecks = symbol.hasSecurityChecks;
            var hasSEH = symbol.hasSEH;
            var hasSetJump = symbol.hasSetJump;
            //var hasValidPGOCounts = symbol.hasValidPGOCounts;
            var hfaDouble = symbol.hfaDouble;
            var hfaFloat = symbol.hfaFloat;
            var indirectVirtualBaseClass = symbol.indirectVirtualBaseClass;
            var inlSpec = symbol.inlSpec;
            var interruptReturn = symbol.interruptReturn;
            var intrinsic = symbol.intrinsic;
            var intro = symbol.intro;
            var isAggregated = symbol.isAggregated;
            var isConstructorVirtualBase = symbol.isConstructorVirtualBase;
            var isCTypes = symbol.isCTypes;
            var isCVTCIL = symbol.isCVTCIL;
            var isCxxReturnUdt = symbol.isCxxReturnUdt;
            var isDataAligned = symbol.isDataAligned;
            var isHLSLData = symbol.isHLSLData;
            var isHotpatchable = symbol.isHotpatchable;
            var isInterfaceUdt = symbol.isInterfaceUdt;
            var isLocationControlFlowDependent = symbol.isLocationControlFlowDependent;
            var isLTCG = symbol.isLTCG;
            var isMatrixRowMajor = symbol.isMatrixRowMajor;
            var isMSILNetmodule = symbol.isMSILNetmodule;
            var isMultipleInheritance = symbol.isMultipleInheritance;
            var isNaked = symbol.isNaked;
            var isOptimizedAway = symbol.isOptimizedAway;
            //var isOptimizedForSpeed = symbol.isOptimizedForSpeed;
            //var isPGO = symbol.isPGO;
            var isPointerBasedOnSymbolValue = symbol.isPointerBasedOnSymbolValue;
            var isPointerToDataMember = symbol.isPointerToDataMember;
            var isPointerToMemberFunction = symbol.isPointerToMemberFunction;
            var isRefUdt = symbol.isRefUdt;
            var isReturnValue = symbol.isReturnValue;
            var isSafeBuffers = symbol.isSafeBuffers;
            var isSdl = symbol.isSdl;
            var isSingleInheritance = symbol.isSingleInheritance;
            var isSplitted = symbol.isSplitted;
            var isStatic = symbol.isStatic;
            var isStripped = symbol.isStripped;
            var isValueUdt = symbol.isValueUdt;
            var isVirtualInheritance = symbol.isVirtualInheritance;
            var isWinRTPointer = symbol.isWinRTPointer;
            var language = symbol.language;
            var length = symbol.length;
            var lexicalParent = symbol.lexicalParent;
            var lexicalParentId = symbol.lexicalParentId;
            var libraryName = symbol.libraryName;
            var liveRangeLength = symbol.liveRangeLength;
            var liveRangeStartAddressOffset = symbol.liveRangeStartAddressOffset;
            var liveRangeStartAddressSection = symbol.liveRangeStartAddressSection;
            var liveRangeStartRelativeVirtualAddress = symbol.liveRangeStartRelativeVirtualAddress;
            var localBasePointerRegisterId = symbol.localBasePointerRegisterId;
            var locationType = symbol.locationType;
            var lowerBound = symbol.lowerBound;
            var lowerBoundId = symbol.lowerBoundId;
            var machineType = symbol.machineType;
            var managed = symbol.managed;
            var memorySpaceKind = symbol.memorySpaceKind;
            var msil = symbol.msil;
            var name = symbol.name;
            var nested = symbol.nested;
            var noInline = symbol.noInline;
            var noNameExport = symbol.noNameExport;
            var noReturn = symbol.noReturn;
            var noStackOrdering = symbol.noStackOrdering;
            var notReached = symbol.notReached;
            var numberOfColumns = symbol.numberOfColumns;
            var numberOfModifiers = symbol.numberOfModifiers;
            var numberOfRegisterIndices = symbol.numberOfRegisterIndices;
            var numberOfRows = symbol.numberOfRows;
            var objectFileName = symbol.objectFileName;
            var objectPointerType = symbol.objectPointerType;
            var oemId = symbol.oemId;
            var oemSymbolId = symbol.oemSymbolId;
            var offset = symbol.offset;
            var offsetInUdt = symbol.offsetInUdt;
            var optimizedCodeDebugInfo = symbol.optimizedCodeDebugInfo;
            var ordinal = symbol.ordinal;
            var overloadedOperator = symbol.overloadedOperator;
            var packed = symbol.packed;
            var paramBasePointerRegisterId = symbol.paramBasePointerRegisterId;
            //var PGODynamicInstructionCount = symbol.PGODynamicInstructionCount;
            //var PGOEdgeCount = symbol.PGOEdgeCount;
            //var PGOEntryCount = symbol.PGOEntryCount;
            var phaseName = symbol.phaseName;
            var platform = symbol.platform;
            var privateExport = symbol.privateExport;
            var pure = symbol.pure;
            var rank = symbol.rank;
            var reference = symbol.reference;
            var registerId = symbol.registerId;
            var registerType = symbol.registerType;
            var relativeVirtualAddress = symbol.relativeVirtualAddress;
            var restrictedType = symbol.restrictedType;
            var RValueReference = symbol.RValueReference;
            var samplerSlot = symbol.samplerSlot;
            var scoped = symbol.scoped;
            var @sealed = symbol.@sealed;
            var signature = symbol.signature;
            var sizeInUdt = symbol.sizeInUdt;
            var slot = symbol.slot;
            var sourceFileName = symbol.sourceFileName;
            var staticSize = symbol.staticSize;
            var strictGSCheck = symbol.strictGSCheck;
            var stride = symbol.stride;
            var subType = symbol.subType;
            var subTypeId = symbol.subTypeId;
            var symbolsFileName = symbol.symbolsFileName;
            var symIndexId = symbol.symIndexId;
            var symTag = symbol.symTag;
            var targetOffset = symbol.targetOffset;
            var targetRelativeVirtualAddress = symbol.targetRelativeVirtualAddress;
            var targetSection = symbol.targetSection;
            var targetVirtualAddress = symbol.targetVirtualAddress;
            var textureSlot = symbol.textureSlot;
            var thisAdjust = symbol.thisAdjust;
            var thunkOrdinal = symbol.thunkOrdinal;
            var timeStamp = symbol.timeStamp;
            var token = symbol.token;
            var type = symbol.type;
            var typeId = symbol.typeId;
            var uavSlot = symbol.uavSlot;
            var udtKind = symbol.udtKind;
            var unalignedType = symbol.unalignedType;
            var undecoratedName = symbol.undecoratedName;
            var unmodifiedType = symbol.unmodifiedType;
            var unmodifiedTypeId = symbol.unmodifiedTypeId;
            //var unused = symbol.unused;
            var upperBound = symbol.upperBound;
            var upperBoundId = symbol.upperBoundId;
            var value = symbol.value;
            var @virtual = symbol.@virtual;
            var virtualAddress = symbol.virtualAddress;
            var virtualBaseClass = symbol.virtualBaseClass;
            var virtualBaseDispIndex = symbol.virtualBaseDispIndex;
            var virtualBaseOffset = symbol.virtualBaseOffset;
            var virtualBasePointerOffset = symbol.virtualBasePointerOffset;
            var virtualBaseTableType = symbol.virtualBaseTableType;
            var virtualTableShape = symbol.virtualTableShape;
            var virtualTableShapeId = symbol.virtualTableShapeId;
            var volatileType = symbol.volatileType;
            var wasInlined = symbol.wasInlined;
            return false;
        }
#endif


        public enum CV_HREG_e {
            // Register subset shared by all processor types,
            // must not overlap with any of the ranges below, hence the high values

            CV_ALLREG_ERR = 30000,
            CV_ALLREG_TEB = 30001,
            CV_ALLREG_TIMER = 30002,
            CV_ALLREG_EFAD1 = 30003,
            CV_ALLREG_EFAD2 = 30004,
            CV_ALLREG_EFAD3 = 30005,
            CV_ALLREG_VFRAME = 30006,
            CV_ALLREG_HANDLE = 30007,
            CV_ALLREG_PARAMS = 30008,
            CV_ALLREG_LOCALS = 30009,
            CV_ALLREG_TID = 30010,
            CV_ALLREG_ENV = 30011,
            CV_ALLREG_CMDLN = 30012,


            //  Register set for the Intel 80x86 and ix86 processor series
            //  (plus PCODE registers)

            CV_REG_NONE = 0,
            CV_REG_AL = 1,
            CV_REG_CL = 2,
            CV_REG_DL = 3,
            CV_REG_BL = 4,
            CV_REG_AH = 5,
            CV_REG_CH = 6,
            CV_REG_DH = 7,
            CV_REG_BH = 8,
            CV_REG_AX = 9,
            CV_REG_CX = 10,
            CV_REG_DX = 11,
            CV_REG_BX = 12,
            CV_REG_SP = 13,
            CV_REG_BP = 14,
            CV_REG_SI = 15,
            CV_REG_DI = 16,
            CV_REG_EAX = 17,
            CV_REG_ECX = 18,
            CV_REG_EDX = 19,
            CV_REG_EBX = 20,
            CV_REG_ESP = 21,
            CV_REG_EBP = 22,
            CV_REG_ESI = 23,
            CV_REG_EDI = 24,
            CV_REG_ES = 25,
            CV_REG_CS = 26,
            CV_REG_SS = 27,
            CV_REG_DS = 28,
            CV_REG_FS = 29,
            CV_REG_GS = 30,
            CV_REG_IP = 31,
            CV_REG_FLAGS = 32,
            CV_REG_EIP = 33,
            CV_REG_EFLAGS = 34,
            CV_REG_TEMP = 40,          // PCODE Temp
            CV_REG_TEMPH = 41,          // PCODE TempH
            CV_REG_QUOTE = 42,          // PCODE Quote
            CV_REG_PCDR3 = 43,          // PCODE reserved
            CV_REG_PCDR4 = 44,          // PCODE reserved
            CV_REG_PCDR5 = 45,          // PCODE reserved
            CV_REG_PCDR6 = 46,          // PCODE reserved
            CV_REG_PCDR7 = 47,          // PCODE reserved
            CV_REG_CR0 = 80,          // CR0 -- control registers
            CV_REG_CR1 = 81,
            CV_REG_CR2 = 82,
            CV_REG_CR3 = 83,
            CV_REG_CR4 = 84,          // Pentium
            CV_REG_DR0 = 90,          // Debug register
            CV_REG_DR1 = 91,
            CV_REG_DR2 = 92,
            CV_REG_DR3 = 93,
            CV_REG_DR4 = 94,
            CV_REG_DR5 = 95,
            CV_REG_DR6 = 96,
            CV_REG_DR7 = 97,
            CV_REG_GDTR = 110,
            CV_REG_GDTL = 111,
            CV_REG_IDTR = 112,
            CV_REG_IDTL = 113,
            CV_REG_LDTR = 114,
            CV_REG_TR = 115,

            CV_REG_PSEUDO1 = 116,
            CV_REG_PSEUDO2 = 117,
            CV_REG_PSEUDO3 = 118,
            CV_REG_PSEUDO4 = 119,
            CV_REG_PSEUDO5 = 120,
            CV_REG_PSEUDO6 = 121,
            CV_REG_PSEUDO7 = 122,
            CV_REG_PSEUDO8 = 123,
            CV_REG_PSEUDO9 = 124,

            CV_REG_ST0 = 128,
            CV_REG_ST1 = 129,
            CV_REG_ST2 = 130,
            CV_REG_ST3 = 131,
            CV_REG_ST4 = 132,
            CV_REG_ST5 = 133,
            CV_REG_ST6 = 134,
            CV_REG_ST7 = 135,
            CV_REG_CTRL = 136,
            CV_REG_STAT = 137,
            CV_REG_TAG = 138,
            CV_REG_FPIP = 139,
            CV_REG_FPCS = 140,
            CV_REG_FPDO = 141,
            CV_REG_FPDS = 142,
            CV_REG_ISEM = 143,
            CV_REG_FPEIP = 144,
            CV_REG_FPEDO = 145,

            CV_REG_MM0 = 146,
            CV_REG_MM1 = 147,
            CV_REG_MM2 = 148,
            CV_REG_MM3 = 149,
            CV_REG_MM4 = 150,
            CV_REG_MM5 = 151,
            CV_REG_MM6 = 152,
            CV_REG_MM7 = 153,

            CV_REG_XMM0 = 154, // KATMAI registers
            CV_REG_XMM1 = 155,
            CV_REG_XMM2 = 156,
            CV_REG_XMM3 = 157,
            CV_REG_XMM4 = 158,
            CV_REG_XMM5 = 159,
            CV_REG_XMM6 = 160,
            CV_REG_XMM7 = 161,

            CV_REG_XMM00 = 162, // KATMAI sub-registers
            CV_REG_XMM01 = 163,
            CV_REG_XMM02 = 164,
            CV_REG_XMM03 = 165,
            CV_REG_XMM10 = 166,
            CV_REG_XMM11 = 167,
            CV_REG_XMM12 = 168,
            CV_REG_XMM13 = 169,
            CV_REG_XMM20 = 170,
            CV_REG_XMM21 = 171,
            CV_REG_XMM22 = 172,
            CV_REG_XMM23 = 173,
            CV_REG_XMM30 = 174,
            CV_REG_XMM31 = 175,
            CV_REG_XMM32 = 176,
            CV_REG_XMM33 = 177,
            CV_REG_XMM40 = 178,
            CV_REG_XMM41 = 179,
            CV_REG_XMM42 = 180,
            CV_REG_XMM43 = 181,
            CV_REG_XMM50 = 182,
            CV_REG_XMM51 = 183,
            CV_REG_XMM52 = 184,
            CV_REG_XMM53 = 185,
            CV_REG_XMM60 = 186,
            CV_REG_XMM61 = 187,
            CV_REG_XMM62 = 188,
            CV_REG_XMM63 = 189,
            CV_REG_XMM70 = 190,
            CV_REG_XMM71 = 191,
            CV_REG_XMM72 = 192,
            CV_REG_XMM73 = 193,

            CV_REG_XMM0L = 194,
            CV_REG_XMM1L = 195,
            CV_REG_XMM2L = 196,
            CV_REG_XMM3L = 197,
            CV_REG_XMM4L = 198,
            CV_REG_XMM5L = 199,
            CV_REG_XMM6L = 200,
            CV_REG_XMM7L = 201,

            CV_REG_XMM0H = 202,
            CV_REG_XMM1H = 203,
            CV_REG_XMM2H = 204,
            CV_REG_XMM3H = 205,
            CV_REG_XMM4H = 206,
            CV_REG_XMM5H = 207,
            CV_REG_XMM6H = 208,
            CV_REG_XMM7H = 209,

            CV_REG_MXCSR = 211, // XMM status register

            CV_REG_EDXEAX = 212, // EDX:EAX pair

            CV_REG_EMM0L = 220, // XMM sub-registers (WNI integer)
            CV_REG_EMM1L = 221,
            CV_REG_EMM2L = 222,
            CV_REG_EMM3L = 223,
            CV_REG_EMM4L = 224,
            CV_REG_EMM5L = 225,
            CV_REG_EMM6L = 226,
            CV_REG_EMM7L = 227,

            CV_REG_EMM0H = 228,
            CV_REG_EMM1H = 229,
            CV_REG_EMM2H = 230,
            CV_REG_EMM3H = 231,
            CV_REG_EMM4H = 232,
            CV_REG_EMM5H = 233,
            CV_REG_EMM6H = 234,
            CV_REG_EMM7H = 235,

            // do not change the order of these regs, first one must be even too
            CV_REG_MM00 = 236,
            CV_REG_MM01 = 237,
            CV_REG_MM10 = 238,
            CV_REG_MM11 = 239,
            CV_REG_MM20 = 240,
            CV_REG_MM21 = 241,
            CV_REG_MM30 = 242,
            CV_REG_MM31 = 243,
            CV_REG_MM40 = 244,
            CV_REG_MM41 = 245,
            CV_REG_MM50 = 246,
            CV_REG_MM51 = 247,
            CV_REG_MM60 = 248,
            CV_REG_MM61 = 249,
            CV_REG_MM70 = 250,
            CV_REG_MM71 = 251,

            CV_REG_YMM0 = 252, // AVX registers
            CV_REG_YMM1 = 253,
            CV_REG_YMM2 = 254,
            CV_REG_YMM3 = 255,
            CV_REG_YMM4 = 256,
            CV_REG_YMM5 = 257,
            CV_REG_YMM6 = 258,
            CV_REG_YMM7 = 259,

            CV_REG_YMM0H = 260,
            CV_REG_YMM1H = 261,
            CV_REG_YMM2H = 262,
            CV_REG_YMM3H = 263,
            CV_REG_YMM4H = 264,
            CV_REG_YMM5H = 265,
            CV_REG_YMM6H = 266,
            CV_REG_YMM7H = 267,

            CV_REG_YMM0I0 = 268,    // AVX integer registers
            CV_REG_YMM0I1 = 269,
            CV_REG_YMM0I2 = 270,
            CV_REG_YMM0I3 = 271,
            CV_REG_YMM1I0 = 272,
            CV_REG_YMM1I1 = 273,
            CV_REG_YMM1I2 = 274,
            CV_REG_YMM1I3 = 275,
            CV_REG_YMM2I0 = 276,
            CV_REG_YMM2I1 = 277,
            CV_REG_YMM2I2 = 278,
            CV_REG_YMM2I3 = 279,
            CV_REG_YMM3I0 = 280,
            CV_REG_YMM3I1 = 281,
            CV_REG_YMM3I2 = 282,
            CV_REG_YMM3I3 = 283,
            CV_REG_YMM4I0 = 284,
            CV_REG_YMM4I1 = 285,
            CV_REG_YMM4I2 = 286,
            CV_REG_YMM4I3 = 287,
            CV_REG_YMM5I0 = 288,
            CV_REG_YMM5I1 = 289,
            CV_REG_YMM5I2 = 290,
            CV_REG_YMM5I3 = 291,
            CV_REG_YMM6I0 = 292,
            CV_REG_YMM6I1 = 293,
            CV_REG_YMM6I2 = 294,
            CV_REG_YMM6I3 = 295,
            CV_REG_YMM7I0 = 296,
            CV_REG_YMM7I1 = 297,
            CV_REG_YMM7I2 = 298,
            CV_REG_YMM7I3 = 299,

            CV_REG_YMM0F0 = 300,     // AVX floating-point single precise registers
            CV_REG_YMM0F1 = 301,
            CV_REG_YMM0F2 = 302,
            CV_REG_YMM0F3 = 303,
            CV_REG_YMM0F4 = 304,
            CV_REG_YMM0F5 = 305,
            CV_REG_YMM0F6 = 306,
            CV_REG_YMM0F7 = 307,
            CV_REG_YMM1F0 = 308,
            CV_REG_YMM1F1 = 309,
            CV_REG_YMM1F2 = 310,
            CV_REG_YMM1F3 = 311,
            CV_REG_YMM1F4 = 312,
            CV_REG_YMM1F5 = 313,
            CV_REG_YMM1F6 = 314,
            CV_REG_YMM1F7 = 315,
            CV_REG_YMM2F0 = 316,
            CV_REG_YMM2F1 = 317,
            CV_REG_YMM2F2 = 318,
            CV_REG_YMM2F3 = 319,
            CV_REG_YMM2F4 = 320,
            CV_REG_YMM2F5 = 321,
            CV_REG_YMM2F6 = 322,
            CV_REG_YMM2F7 = 323,
            CV_REG_YMM3F0 = 324,
            CV_REG_YMM3F1 = 325,
            CV_REG_YMM3F2 = 326,
            CV_REG_YMM3F3 = 327,
            CV_REG_YMM3F4 = 328,
            CV_REG_YMM3F5 = 329,
            CV_REG_YMM3F6 = 330,
            CV_REG_YMM3F7 = 331,
            CV_REG_YMM4F0 = 332,
            CV_REG_YMM4F1 = 333,
            CV_REG_YMM4F2 = 334,
            CV_REG_YMM4F3 = 335,
            CV_REG_YMM4F4 = 336,
            CV_REG_YMM4F5 = 337,
            CV_REG_YMM4F6 = 338,
            CV_REG_YMM4F7 = 339,
            CV_REG_YMM5F0 = 340,
            CV_REG_YMM5F1 = 341,
            CV_REG_YMM5F2 = 342,
            CV_REG_YMM5F3 = 343,
            CV_REG_YMM5F4 = 344,
            CV_REG_YMM5F5 = 345,
            CV_REG_YMM5F6 = 346,
            CV_REG_YMM5F7 = 347,
            CV_REG_YMM6F0 = 348,
            CV_REG_YMM6F1 = 349,
            CV_REG_YMM6F2 = 350,
            CV_REG_YMM6F3 = 351,
            CV_REG_YMM6F4 = 352,
            CV_REG_YMM6F5 = 353,
            CV_REG_YMM6F6 = 354,
            CV_REG_YMM6F7 = 355,
            CV_REG_YMM7F0 = 356,
            CV_REG_YMM7F1 = 357,
            CV_REG_YMM7F2 = 358,
            CV_REG_YMM7F3 = 359,
            CV_REG_YMM7F4 = 360,
            CV_REG_YMM7F5 = 361,
            CV_REG_YMM7F6 = 362,
            CV_REG_YMM7F7 = 363,

            CV_REG_YMM0D0 = 364,    // AVX floating-point double precise registers
            CV_REG_YMM0D1 = 365,
            CV_REG_YMM0D2 = 366,
            CV_REG_YMM0D3 = 367,
            CV_REG_YMM1D0 = 368,
            CV_REG_YMM1D1 = 369,
            CV_REG_YMM1D2 = 370,
            CV_REG_YMM1D3 = 371,
            CV_REG_YMM2D0 = 372,
            CV_REG_YMM2D1 = 373,
            CV_REG_YMM2D2 = 374,
            CV_REG_YMM2D3 = 375,
            CV_REG_YMM3D0 = 376,
            CV_REG_YMM3D1 = 377,
            CV_REG_YMM3D2 = 378,
            CV_REG_YMM3D3 = 379,
            CV_REG_YMM4D0 = 380,
            CV_REG_YMM4D1 = 381,
            CV_REG_YMM4D2 = 382,
            CV_REG_YMM4D3 = 383,
            CV_REG_YMM5D0 = 384,
            CV_REG_YMM5D1 = 385,
            CV_REG_YMM5D2 = 386,
            CV_REG_YMM5D3 = 387,
            CV_REG_YMM6D0 = 388,
            CV_REG_YMM6D1 = 389,
            CV_REG_YMM6D2 = 390,
            CV_REG_YMM6D3 = 391,
            CV_REG_YMM7D0 = 392,
            CV_REG_YMM7D1 = 393,
            CV_REG_YMM7D2 = 394,
            CV_REG_YMM7D3 = 395,

            CV_REG_BND0 = 396,
            CV_REG_BND1 = 397,
            CV_REG_BND2 = 398,
            CV_REG_BND3 = 399,

            // registers for the 68K processors

            CV_R68_D0 = 0,
            CV_R68_D1 = 1,
            CV_R68_D2 = 2,
            CV_R68_D3 = 3,
            CV_R68_D4 = 4,
            CV_R68_D5 = 5,
            CV_R68_D6 = 6,
            CV_R68_D7 = 7,
            CV_R68_A0 = 8,
            CV_R68_A1 = 9,
            CV_R68_A2 = 10,
            CV_R68_A3 = 11,
            CV_R68_A4 = 12,
            CV_R68_A5 = 13,
            CV_R68_A6 = 14,
            CV_R68_A7 = 15,
            CV_R68_CCR = 16,
            CV_R68_SR = 17,
            CV_R68_USP = 18,
            CV_R68_MSP = 19,
            CV_R68_SFC = 20,
            CV_R68_DFC = 21,
            CV_R68_CACR = 22,
            CV_R68_VBR = 23,
            CV_R68_CAAR = 24,
            CV_R68_ISP = 25,
            CV_R68_PC = 26,
            //reserved  27
            CV_R68_FPCR = 28,
            CV_R68_FPSR = 29,
            CV_R68_FPIAR = 30,
            //reserved  31
            CV_R68_FP0 = 32,
            CV_R68_FP1 = 33,
            CV_R68_FP2 = 34,
            CV_R68_FP3 = 35,
            CV_R68_FP4 = 36,
            CV_R68_FP5 = 37,
            CV_R68_FP6 = 38,
            CV_R68_FP7 = 39,
            //reserved  40
            CV_R68_MMUSR030 = 41,
            CV_R68_MMUSR = 42,
            CV_R68_URP = 43,
            CV_R68_DTT0 = 44,
            CV_R68_DTT1 = 45,
            CV_R68_ITT0 = 46,
            CV_R68_ITT1 = 47,
            //reserved  50
            CV_R68_PSR = 51,
            CV_R68_PCSR = 52,
            CV_R68_VAL = 53,
            CV_R68_CRP = 54,
            CV_R68_SRP = 55,
            CV_R68_DRP = 56,
            CV_R68_TC = 57,
            CV_R68_AC = 58,
            CV_R68_SCC = 59,
            CV_R68_CAL = 60,
            CV_R68_TT0 = 61,
            CV_R68_TT1 = 62,
            //reserved  63
            CV_R68_BAD0 = 64,
            CV_R68_BAD1 = 65,
            CV_R68_BAD2 = 66,
            CV_R68_BAD3 = 67,
            CV_R68_BAD4 = 68,
            CV_R68_BAD5 = 69,
            CV_R68_BAD6 = 70,
            CV_R68_BAD7 = 71,
            CV_R68_BAC0 = 72,
            CV_R68_BAC1 = 73,
            CV_R68_BAC2 = 74,
            CV_R68_BAC3 = 75,
            CV_R68_BAC4 = 76,
            CV_R68_BAC5 = 77,
            CV_R68_BAC6 = 78,
            CV_R68_BAC7 = 79,

            // Register set for the MIPS 4000

            CV_M4_NOREG = CV_REG_NONE,

            CV_M4_IntZERO = 10,      /* CPU REGISTER */
            CV_M4_IntAT = 11,
            CV_M4_IntV0 = 12,
            CV_M4_IntV1 = 13,
            CV_M4_IntA0 = 14,
            CV_M4_IntA1 = 15,
            CV_M4_IntA2 = 16,
            CV_M4_IntA3 = 17,
            CV_M4_IntT0 = 18,
            CV_M4_IntT1 = 19,
            CV_M4_IntT2 = 20,
            CV_M4_IntT3 = 21,
            CV_M4_IntT4 = 22,
            CV_M4_IntT5 = 23,
            CV_M4_IntT6 = 24,
            CV_M4_IntT7 = 25,
            CV_M4_IntS0 = 26,
            CV_M4_IntS1 = 27,
            CV_M4_IntS2 = 28,
            CV_M4_IntS3 = 29,
            CV_M4_IntS4 = 30,
            CV_M4_IntS5 = 31,
            CV_M4_IntS6 = 32,
            CV_M4_IntS7 = 33,
            CV_M4_IntT8 = 34,
            CV_M4_IntT9 = 35,
            CV_M4_IntKT0 = 36,
            CV_M4_IntKT1 = 37,
            CV_M4_IntGP = 38,
            CV_M4_IntSP = 39,
            CV_M4_IntS8 = 40,
            CV_M4_IntRA = 41,
            CV_M4_IntLO = 42,
            CV_M4_IntHI = 43,

            CV_M4_Fir = 50,
            CV_M4_Psr = 51,

            CV_M4_FltF0 = 60,      /* Floating point registers */
            CV_M4_FltF1 = 61,
            CV_M4_FltF2 = 62,
            CV_M4_FltF3 = 63,
            CV_M4_FltF4 = 64,
            CV_M4_FltF5 = 65,
            CV_M4_FltF6 = 66,
            CV_M4_FltF7 = 67,
            CV_M4_FltF8 = 68,
            CV_M4_FltF9 = 69,
            CV_M4_FltF10 = 70,
            CV_M4_FltF11 = 71,
            CV_M4_FltF12 = 72,
            CV_M4_FltF13 = 73,
            CV_M4_FltF14 = 74,
            CV_M4_FltF15 = 75,
            CV_M4_FltF16 = 76,
            CV_M4_FltF17 = 77,
            CV_M4_FltF18 = 78,
            CV_M4_FltF19 = 79,
            CV_M4_FltF20 = 80,
            CV_M4_FltF21 = 81,
            CV_M4_FltF22 = 82,
            CV_M4_FltF23 = 83,
            CV_M4_FltF24 = 84,
            CV_M4_FltF25 = 85,
            CV_M4_FltF26 = 86,
            CV_M4_FltF27 = 87,
            CV_M4_FltF28 = 88,
            CV_M4_FltF29 = 89,
            CV_M4_FltF30 = 90,
            CV_M4_FltF31 = 91,
            CV_M4_FltFsr = 92,


            // Register set for the ALPHA AXP

            CV_ALPHA_NOREG = CV_REG_NONE,

            CV_ALPHA_FltF0 = 10,   // Floating point registers
            CV_ALPHA_FltF1 = 11,
            CV_ALPHA_FltF2 = 12,
            CV_ALPHA_FltF3 = 13,
            CV_ALPHA_FltF4 = 14,
            CV_ALPHA_FltF5 = 15,
            CV_ALPHA_FltF6 = 16,
            CV_ALPHA_FltF7 = 17,
            CV_ALPHA_FltF8 = 18,
            CV_ALPHA_FltF9 = 19,
            CV_ALPHA_FltF10 = 20,
            CV_ALPHA_FltF11 = 21,
            CV_ALPHA_FltF12 = 22,
            CV_ALPHA_FltF13 = 23,
            CV_ALPHA_FltF14 = 24,
            CV_ALPHA_FltF15 = 25,
            CV_ALPHA_FltF16 = 26,
            CV_ALPHA_FltF17 = 27,
            CV_ALPHA_FltF18 = 28,
            CV_ALPHA_FltF19 = 29,
            CV_ALPHA_FltF20 = 30,
            CV_ALPHA_FltF21 = 31,
            CV_ALPHA_FltF22 = 32,
            CV_ALPHA_FltF23 = 33,
            CV_ALPHA_FltF24 = 34,
            CV_ALPHA_FltF25 = 35,
            CV_ALPHA_FltF26 = 36,
            CV_ALPHA_FltF27 = 37,
            CV_ALPHA_FltF28 = 38,
            CV_ALPHA_FltF29 = 39,
            CV_ALPHA_FltF30 = 40,
            CV_ALPHA_FltF31 = 41,

            CV_ALPHA_IntV0 = 42,   // Integer registers
            CV_ALPHA_IntT0 = 43,
            CV_ALPHA_IntT1 = 44,
            CV_ALPHA_IntT2 = 45,
            CV_ALPHA_IntT3 = 46,
            CV_ALPHA_IntT4 = 47,
            CV_ALPHA_IntT5 = 48,
            CV_ALPHA_IntT6 = 49,
            CV_ALPHA_IntT7 = 50,
            CV_ALPHA_IntS0 = 51,
            CV_ALPHA_IntS1 = 52,
            CV_ALPHA_IntS2 = 53,
            CV_ALPHA_IntS3 = 54,
            CV_ALPHA_IntS4 = 55,
            CV_ALPHA_IntS5 = 56,
            CV_ALPHA_IntFP = 57,
            CV_ALPHA_IntA0 = 58,
            CV_ALPHA_IntA1 = 59,
            CV_ALPHA_IntA2 = 60,
            CV_ALPHA_IntA3 = 61,
            CV_ALPHA_IntA4 = 62,
            CV_ALPHA_IntA5 = 63,
            CV_ALPHA_IntT8 = 64,
            CV_ALPHA_IntT9 = 65,
            CV_ALPHA_IntT10 = 66,
            CV_ALPHA_IntT11 = 67,
            CV_ALPHA_IntRA = 68,
            CV_ALPHA_IntT12 = 69,
            CV_ALPHA_IntAT = 70,
            CV_ALPHA_IntGP = 71,
            CV_ALPHA_IntSP = 72,
            CV_ALPHA_IntZERO = 73,


            CV_ALPHA_Fpcr = 74,   // Control registers
            CV_ALPHA_Fir = 75,
            CV_ALPHA_Psr = 76,
            CV_ALPHA_FltFsr = 77,
            CV_ALPHA_SoftFpcr = 78,

            // Register Set for Motorola/IBM PowerPC

            /*
            ** PowerPC General Registers ( User Level )
            */
            CV_PPC_GPR0 = 1,
            CV_PPC_GPR1 = 2,
            CV_PPC_GPR2 = 3,
            CV_PPC_GPR3 = 4,
            CV_PPC_GPR4 = 5,
            CV_PPC_GPR5 = 6,
            CV_PPC_GPR6 = 7,
            CV_PPC_GPR7 = 8,
            CV_PPC_GPR8 = 9,
            CV_PPC_GPR9 = 10,
            CV_PPC_GPR10 = 11,
            CV_PPC_GPR11 = 12,
            CV_PPC_GPR12 = 13,
            CV_PPC_GPR13 = 14,
            CV_PPC_GPR14 = 15,
            CV_PPC_GPR15 = 16,
            CV_PPC_GPR16 = 17,
            CV_PPC_GPR17 = 18,
            CV_PPC_GPR18 = 19,
            CV_PPC_GPR19 = 20,
            CV_PPC_GPR20 = 21,
            CV_PPC_GPR21 = 22,
            CV_PPC_GPR22 = 23,
            CV_PPC_GPR23 = 24,
            CV_PPC_GPR24 = 25,
            CV_PPC_GPR25 = 26,
            CV_PPC_GPR26 = 27,
            CV_PPC_GPR27 = 28,
            CV_PPC_GPR28 = 29,
            CV_PPC_GPR29 = 30,
            CV_PPC_GPR30 = 31,
            CV_PPC_GPR31 = 32,

            /*
            ** PowerPC Condition Register ( User Level )
            */
            CV_PPC_CR = 33,
            CV_PPC_CR0 = 34,
            CV_PPC_CR1 = 35,
            CV_PPC_CR2 = 36,
            CV_PPC_CR3 = 37,
            CV_PPC_CR4 = 38,
            CV_PPC_CR5 = 39,
            CV_PPC_CR6 = 40,
            CV_PPC_CR7 = 41,

            /*
            ** PowerPC Floating Point Registers ( User Level )
            */
            CV_PPC_FPR0 = 42,
            CV_PPC_FPR1 = 43,
            CV_PPC_FPR2 = 44,
            CV_PPC_FPR3 = 45,
            CV_PPC_FPR4 = 46,
            CV_PPC_FPR5 = 47,
            CV_PPC_FPR6 = 48,
            CV_PPC_FPR7 = 49,
            CV_PPC_FPR8 = 50,
            CV_PPC_FPR9 = 51,
            CV_PPC_FPR10 = 52,
            CV_PPC_FPR11 = 53,
            CV_PPC_FPR12 = 54,
            CV_PPC_FPR13 = 55,
            CV_PPC_FPR14 = 56,
            CV_PPC_FPR15 = 57,
            CV_PPC_FPR16 = 58,
            CV_PPC_FPR17 = 59,
            CV_PPC_FPR18 = 60,
            CV_PPC_FPR19 = 61,
            CV_PPC_FPR20 = 62,
            CV_PPC_FPR21 = 63,
            CV_PPC_FPR22 = 64,
            CV_PPC_FPR23 = 65,
            CV_PPC_FPR24 = 66,
            CV_PPC_FPR25 = 67,
            CV_PPC_FPR26 = 68,
            CV_PPC_FPR27 = 69,
            CV_PPC_FPR28 = 70,
            CV_PPC_FPR29 = 71,
            CV_PPC_FPR30 = 72,
            CV_PPC_FPR31 = 73,

            /*
            ** PowerPC Floating Point Status and Control Register ( User Level )
            */
            CV_PPC_FPSCR = 74,

            /*
            ** PowerPC Machine State Register ( Supervisor Level )
            */
            CV_PPC_MSR = 75,

            /*
            ** PowerPC Segment Registers ( Supervisor Level )
            */
            CV_PPC_SR0 = 76,
            CV_PPC_SR1 = 77,
            CV_PPC_SR2 = 78,
            CV_PPC_SR3 = 79,
            CV_PPC_SR4 = 80,
            CV_PPC_SR5 = 81,
            CV_PPC_SR6 = 82,
            CV_PPC_SR7 = 83,
            CV_PPC_SR8 = 84,
            CV_PPC_SR9 = 85,
            CV_PPC_SR10 = 86,
            CV_PPC_SR11 = 87,
            CV_PPC_SR12 = 88,
            CV_PPC_SR13 = 89,
            CV_PPC_SR14 = 90,
            CV_PPC_SR15 = 91,

            /*
            ** For all of the special purpose registers add 100 to the SPR# that the
            ** Motorola/IBM documentation gives with the exception of any imaginary
            ** registers.
            */

            /*
            ** PowerPC Special Purpose Registers ( User Level )
            */
            CV_PPC_PC = 99,     // PC (imaginary register)

            CV_PPC_MQ = 100,    // MPC601
            CV_PPC_XER = 101,
            CV_PPC_RTCU = 104,    // MPC601
            CV_PPC_RTCL = 105,    // MPC601
            CV_PPC_LR = 108,
            CV_PPC_CTR = 109,

            CV_PPC_COMPARE = 110,    // part of XER (internal to the debugger only)
            CV_PPC_COUNT = 111,    // part of XER (internal to the debugger only)

            /*
            ** PowerPC Special Purpose Registers ( Supervisor Level )
            */
            CV_PPC_DSISR = 118,
            CV_PPC_DAR = 119,
            CV_PPC_DEC = 122,
            CV_PPC_SDR1 = 125,
            CV_PPC_SRR0 = 126,
            CV_PPC_SRR1 = 127,
            CV_PPC_SPRG0 = 372,
            CV_PPC_SPRG1 = 373,
            CV_PPC_SPRG2 = 374,
            CV_PPC_SPRG3 = 375,
            CV_PPC_ASR = 280,    // 64-bit implementations only
            CV_PPC_EAR = 382,
            CV_PPC_PVR = 287,
            CV_PPC_BAT0U = 628,
            CV_PPC_BAT0L = 629,
            CV_PPC_BAT1U = 630,
            CV_PPC_BAT1L = 631,
            CV_PPC_BAT2U = 632,
            CV_PPC_BAT2L = 633,
            CV_PPC_BAT3U = 634,
            CV_PPC_BAT3L = 635,
            CV_PPC_DBAT0U = 636,
            CV_PPC_DBAT0L = 637,
            CV_PPC_DBAT1U = 638,
            CV_PPC_DBAT1L = 639,
            CV_PPC_DBAT2U = 640,
            CV_PPC_DBAT2L = 641,
            CV_PPC_DBAT3U = 642,
            CV_PPC_DBAT3L = 643,

            /*
            ** PowerPC Special Purpose Registers Implementation Dependent ( Supervisor Level )
            */

            /*
            ** Doesn't appear that IBM/Motorola has finished defining these.
            */

            CV_PPC_PMR0 = 1044,   // MPC620,
            CV_PPC_PMR1 = 1045,   // MPC620,
            CV_PPC_PMR2 = 1046,   // MPC620,
            CV_PPC_PMR3 = 1047,   // MPC620,
            CV_PPC_PMR4 = 1048,   // MPC620,
            CV_PPC_PMR5 = 1049,   // MPC620,
            CV_PPC_PMR6 = 1050,   // MPC620,
            CV_PPC_PMR7 = 1051,   // MPC620,
            CV_PPC_PMR8 = 1052,   // MPC620,
            CV_PPC_PMR9 = 1053,   // MPC620,
            CV_PPC_PMR10 = 1054,   // MPC620,
            CV_PPC_PMR11 = 1055,   // MPC620,
            CV_PPC_PMR12 = 1056,   // MPC620,
            CV_PPC_PMR13 = 1057,   // MPC620,
            CV_PPC_PMR14 = 1058,   // MPC620,
            CV_PPC_PMR15 = 1059,   // MPC620,

            CV_PPC_DMISS = 1076,   // MPC603
            CV_PPC_DCMP = 1077,   // MPC603
            CV_PPC_HASH1 = 1078,   // MPC603
            CV_PPC_HASH2 = 1079,   // MPC603
            CV_PPC_IMISS = 1080,   // MPC603
            CV_PPC_ICMP = 1081,   // MPC603
            CV_PPC_RPA = 1082,   // MPC603

            CV_PPC_HID0 = 1108,   // MPC601, MPC603, MPC620
            CV_PPC_HID1 = 1109,   // MPC601
            CV_PPC_HID2 = 1110,   // MPC601, MPC603, MPC620 ( IABR )
            CV_PPC_HID3 = 1111,   // Not Defined
            CV_PPC_HID4 = 1112,   // Not Defined
            CV_PPC_HID5 = 1113,   // MPC601, MPC604, MPC620 ( DABR )
            CV_PPC_HID6 = 1114,   // Not Defined
            CV_PPC_HID7 = 1115,   // Not Defined
            CV_PPC_HID8 = 1116,   // MPC620 ( BUSCSR )
            CV_PPC_HID9 = 1117,   // MPC620 ( L2CSR )
            CV_PPC_HID10 = 1118,   // Not Defined
            CV_PPC_HID11 = 1119,   // Not Defined
            CV_PPC_HID12 = 1120,   // Not Defined
            CV_PPC_HID13 = 1121,   // MPC604 ( HCR )
            CV_PPC_HID14 = 1122,   // Not Defined
            CV_PPC_HID15 = 1123,   // MPC601, MPC604, MPC620 ( PIR )

            //
            // JAVA VM registers
            //

            CV_JAVA_PC = 1,

            //
            // Register set for the Hitachi SH3
            //

            CV_SH3_NOREG = CV_REG_NONE,

            CV_SH3_IntR0 = 10,   // CPU REGISTER
            CV_SH3_IntR1 = 11,
            CV_SH3_IntR2 = 12,
            CV_SH3_IntR3 = 13,
            CV_SH3_IntR4 = 14,
            CV_SH3_IntR5 = 15,
            CV_SH3_IntR6 = 16,
            CV_SH3_IntR7 = 17,
            CV_SH3_IntR8 = 18,
            CV_SH3_IntR9 = 19,
            CV_SH3_IntR10 = 20,
            CV_SH3_IntR11 = 21,
            CV_SH3_IntR12 = 22,
            CV_SH3_IntR13 = 23,
            CV_SH3_IntFp = 24,
            CV_SH3_IntSp = 25,
            CV_SH3_Gbr = 38,
            CV_SH3_Pr = 39,
            CV_SH3_Mach = 40,
            CV_SH3_Macl = 41,

            CV_SH3_Pc = 50,
            CV_SH3_Sr = 51,

            CV_SH3_BarA = 60,
            CV_SH3_BasrA = 61,
            CV_SH3_BamrA = 62,
            CV_SH3_BbrA = 63,
            CV_SH3_BarB = 64,
            CV_SH3_BasrB = 65,
            CV_SH3_BamrB = 66,
            CV_SH3_BbrB = 67,
            CV_SH3_BdrB = 68,
            CV_SH3_BdmrB = 69,
            CV_SH3_Brcr = 70,

            //
            // Additional registers for Hitachi SH processors
            //

            CV_SH_Fpscr = 75,    // floating point status/control register
            CV_SH_Fpul = 76,    // floating point communication register

            CV_SH_FpR0 = 80,    // Floating point registers
            CV_SH_FpR1 = 81,
            CV_SH_FpR2 = 82,
            CV_SH_FpR3 = 83,
            CV_SH_FpR4 = 84,
            CV_SH_FpR5 = 85,
            CV_SH_FpR6 = 86,
            CV_SH_FpR7 = 87,
            CV_SH_FpR8 = 88,
            CV_SH_FpR9 = 89,
            CV_SH_FpR10 = 90,
            CV_SH_FpR11 = 91,
            CV_SH_FpR12 = 92,
            CV_SH_FpR13 = 93,
            CV_SH_FpR14 = 94,
            CV_SH_FpR15 = 95,

            CV_SH_XFpR0 = 96,
            CV_SH_XFpR1 = 97,
            CV_SH_XFpR2 = 98,
            CV_SH_XFpR3 = 99,
            CV_SH_XFpR4 = 100,
            CV_SH_XFpR5 = 101,
            CV_SH_XFpR6 = 102,
            CV_SH_XFpR7 = 103,
            CV_SH_XFpR8 = 104,
            CV_SH_XFpR9 = 105,
            CV_SH_XFpR10 = 106,
            CV_SH_XFpR11 = 107,
            CV_SH_XFpR12 = 108,
            CV_SH_XFpR13 = 109,
            CV_SH_XFpR14 = 110,
            CV_SH_XFpR15 = 111,

            //
            // Register set for the ARM processor.
            //

            CV_ARM_NOREG = CV_REG_NONE,

            CV_ARM_R0 = 10,
            CV_ARM_R1 = 11,
            CV_ARM_R2 = 12,
            CV_ARM_R3 = 13,
            CV_ARM_R4 = 14,
            CV_ARM_R5 = 15,
            CV_ARM_R6 = 16,
            CV_ARM_R7 = 17,
            CV_ARM_R8 = 18,
            CV_ARM_R9 = 19,
            CV_ARM_R10 = 20,
            CV_ARM_R11 = 21, // Frame pointer, if allocated
            CV_ARM_R12 = 22,
            CV_ARM_SP = 23, // Stack pointer
            CV_ARM_LR = 24, // Link Register
            CV_ARM_PC = 25, // Program counter
            CV_ARM_CPSR = 26, // Current program status register

            CV_ARM_ACC0 = 27, // DSP co-processor 0 40 bit accumulator

            //
            // Registers for ARM VFP10 support
            //

            CV_ARM_FPSCR = 40,
            CV_ARM_FPEXC = 41,

            CV_ARM_FS0 = 50,
            CV_ARM_FS1 = 51,
            CV_ARM_FS2 = 52,
            CV_ARM_FS3 = 53,
            CV_ARM_FS4 = 54,
            CV_ARM_FS5 = 55,
            CV_ARM_FS6 = 56,
            CV_ARM_FS7 = 57,
            CV_ARM_FS8 = 58,
            CV_ARM_FS9 = 59,
            CV_ARM_FS10 = 60,
            CV_ARM_FS11 = 61,
            CV_ARM_FS12 = 62,
            CV_ARM_FS13 = 63,
            CV_ARM_FS14 = 64,
            CV_ARM_FS15 = 65,
            CV_ARM_FS16 = 66,
            CV_ARM_FS17 = 67,
            CV_ARM_FS18 = 68,
            CV_ARM_FS19 = 69,
            CV_ARM_FS20 = 70,
            CV_ARM_FS21 = 71,
            CV_ARM_FS22 = 72,
            CV_ARM_FS23 = 73,
            CV_ARM_FS24 = 74,
            CV_ARM_FS25 = 75,
            CV_ARM_FS26 = 76,
            CV_ARM_FS27 = 77,
            CV_ARM_FS28 = 78,
            CV_ARM_FS29 = 79,
            CV_ARM_FS30 = 80,
            CV_ARM_FS31 = 81,

            //
            // ARM VFP Floating Point Extra control registers
            //

            CV_ARM_FPEXTRA0 = 90,
            CV_ARM_FPEXTRA1 = 91,
            CV_ARM_FPEXTRA2 = 92,
            CV_ARM_FPEXTRA3 = 93,
            CV_ARM_FPEXTRA4 = 94,
            CV_ARM_FPEXTRA5 = 95,
            CV_ARM_FPEXTRA6 = 96,
            CV_ARM_FPEXTRA7 = 97,

            // XSCALE Concan co-processor registers
            CV_ARM_WR0 = 128,
            CV_ARM_WR1 = 129,
            CV_ARM_WR2 = 130,
            CV_ARM_WR3 = 131,
            CV_ARM_WR4 = 132,
            CV_ARM_WR5 = 133,
            CV_ARM_WR6 = 134,
            CV_ARM_WR7 = 135,
            CV_ARM_WR8 = 136,
            CV_ARM_WR9 = 137,
            CV_ARM_WR10 = 138,
            CV_ARM_WR11 = 139,
            CV_ARM_WR12 = 140,
            CV_ARM_WR13 = 141,
            CV_ARM_WR14 = 142,
            CV_ARM_WR15 = 143,

            // XSCALE Concan co-processor control registers
            CV_ARM_WCID = 144,
            CV_ARM_WCON = 145,
            CV_ARM_WCSSF = 146,
            CV_ARM_WCASF = 147,
            CV_ARM_WC4 = 148,
            CV_ARM_WC5 = 149,
            CV_ARM_WC6 = 150,
            CV_ARM_WC7 = 151,
            CV_ARM_WCGR0 = 152,
            CV_ARM_WCGR1 = 153,
            CV_ARM_WCGR2 = 154,
            CV_ARM_WCGR3 = 155,
            CV_ARM_WC12 = 156,
            CV_ARM_WC13 = 157,
            CV_ARM_WC14 = 158,
            CV_ARM_WC15 = 159,

            //
            // ARM VFPv3/Neon extended floating Point
            //

            CV_ARM_FS32 = 200,
            CV_ARM_FS33 = 201,
            CV_ARM_FS34 = 202,
            CV_ARM_FS35 = 203,
            CV_ARM_FS36 = 204,
            CV_ARM_FS37 = 205,
            CV_ARM_FS38 = 206,
            CV_ARM_FS39 = 207,
            CV_ARM_FS40 = 208,
            CV_ARM_FS41 = 209,
            CV_ARM_FS42 = 210,
            CV_ARM_FS43 = 211,
            CV_ARM_FS44 = 212,
            CV_ARM_FS45 = 213,
            CV_ARM_FS46 = 214,
            CV_ARM_FS47 = 215,
            CV_ARM_FS48 = 216,
            CV_ARM_FS49 = 217,
            CV_ARM_FS50 = 218,
            CV_ARM_FS51 = 219,
            CV_ARM_FS52 = 220,
            CV_ARM_FS53 = 221,
            CV_ARM_FS54 = 222,
            CV_ARM_FS55 = 223,
            CV_ARM_FS56 = 224,
            CV_ARM_FS57 = 225,
            CV_ARM_FS58 = 226,
            CV_ARM_FS59 = 227,
            CV_ARM_FS60 = 228,
            CV_ARM_FS61 = 229,
            CV_ARM_FS62 = 230,
            CV_ARM_FS63 = 231,

            // ARM double-precision floating point

            CV_ARM_ND0 = 300,
            CV_ARM_ND1 = 301,
            CV_ARM_ND2 = 302,
            CV_ARM_ND3 = 303,
            CV_ARM_ND4 = 304,
            CV_ARM_ND5 = 305,
            CV_ARM_ND6 = 306,
            CV_ARM_ND7 = 307,
            CV_ARM_ND8 = 308,
            CV_ARM_ND9 = 309,
            CV_ARM_ND10 = 310,
            CV_ARM_ND11 = 311,
            CV_ARM_ND12 = 312,
            CV_ARM_ND13 = 313,
            CV_ARM_ND14 = 314,
            CV_ARM_ND15 = 315,
            CV_ARM_ND16 = 316,
            CV_ARM_ND17 = 317,
            CV_ARM_ND18 = 318,
            CV_ARM_ND19 = 319,
            CV_ARM_ND20 = 320,
            CV_ARM_ND21 = 321,
            CV_ARM_ND22 = 322,
            CV_ARM_ND23 = 323,
            CV_ARM_ND24 = 324,
            CV_ARM_ND25 = 325,
            CV_ARM_ND26 = 326,
            CV_ARM_ND27 = 327,
            CV_ARM_ND28 = 328,
            CV_ARM_ND29 = 329,
            CV_ARM_ND30 = 330,
            CV_ARM_ND31 = 331,

            // ARM extended precision floating point

            CV_ARM_NQ0 = 400,
            CV_ARM_NQ1 = 401,
            CV_ARM_NQ2 = 402,
            CV_ARM_NQ3 = 403,
            CV_ARM_NQ4 = 404,
            CV_ARM_NQ5 = 405,
            CV_ARM_NQ6 = 406,
            CV_ARM_NQ7 = 407,
            CV_ARM_NQ8 = 408,
            CV_ARM_NQ9 = 409,
            CV_ARM_NQ10 = 410,
            CV_ARM_NQ11 = 411,
            CV_ARM_NQ12 = 412,
            CV_ARM_NQ13 = 413,
            CV_ARM_NQ14 = 414,
            CV_ARM_NQ15 = 415,

            //
            // Register set for ARM64
            //

            CV_ARM64_NOREG = CV_REG_NONE,

            // General purpose 32-bit integer registers

            CV_ARM64_W0 = 10,
            CV_ARM64_W1 = 11,
            CV_ARM64_W2 = 12,
            CV_ARM64_W3 = 13,
            CV_ARM64_W4 = 14,
            CV_ARM64_W5 = 15,
            CV_ARM64_W6 = 16,
            CV_ARM64_W7 = 17,
            CV_ARM64_W8 = 18,
            CV_ARM64_W9 = 19,
            CV_ARM64_W10 = 20,
            CV_ARM64_W11 = 21,
            CV_ARM64_W12 = 22,
            CV_ARM64_W13 = 23,
            CV_ARM64_W14 = 24,
            CV_ARM64_W15 = 25,
            CV_ARM64_W16 = 26,
            CV_ARM64_W17 = 27,
            CV_ARM64_W18 = 28,
            CV_ARM64_W19 = 29,
            CV_ARM64_W20 = 30,
            CV_ARM64_W21 = 31,
            CV_ARM64_W22 = 32,
            CV_ARM64_W23 = 33,
            CV_ARM64_W24 = 34,
            CV_ARM64_W25 = 35,
            CV_ARM64_W26 = 36,
            CV_ARM64_W27 = 37,
            CV_ARM64_W28 = 38,
            CV_ARM64_W29 = 39,
            CV_ARM64_W30 = 40,
            CV_ARM64_WZR = 41,

            // General purpose 64-bit integer registers

            CV_ARM64_X0 = 50,
            CV_ARM64_X1 = 51,
            CV_ARM64_X2 = 52,
            CV_ARM64_X3 = 53,
            CV_ARM64_X4 = 54,
            CV_ARM64_X5 = 55,
            CV_ARM64_X6 = 56,
            CV_ARM64_X7 = 57,
            CV_ARM64_X8 = 58,
            CV_ARM64_X9 = 59,
            CV_ARM64_X10 = 60,
            CV_ARM64_X11 = 61,
            CV_ARM64_X12 = 62,
            CV_ARM64_X13 = 63,
            CV_ARM64_X14 = 64,
            CV_ARM64_X15 = 65,
            CV_ARM64_IP0 = 66,
            CV_ARM64_IP1 = 67,
            CV_ARM64_X18 = 68,
            CV_ARM64_X19 = 69,
            CV_ARM64_X20 = 70,
            CV_ARM64_X21 = 71,
            CV_ARM64_X22 = 72,
            CV_ARM64_X23 = 73,
            CV_ARM64_X24 = 74,
            CV_ARM64_X25 = 75,
            CV_ARM64_X26 = 76,
            CV_ARM64_X27 = 77,
            CV_ARM64_X28 = 78,
            CV_ARM64_FP = 79,
            CV_ARM64_LR = 80,
            CV_ARM64_SP = 81,
            CV_ARM64_ZR = 82,

            // statue register

            CV_ARM64_NZCV = 90,

            // 32-bit floating point registers

            CV_ARM64_S0 = 100,
            CV_ARM64_S1 = 101,
            CV_ARM64_S2 = 102,
            CV_ARM64_S3 = 103,
            CV_ARM64_S4 = 104,
            CV_ARM64_S5 = 105,
            CV_ARM64_S6 = 106,
            CV_ARM64_S7 = 107,
            CV_ARM64_S8 = 108,
            CV_ARM64_S9 = 109,
            CV_ARM64_S10 = 110,
            CV_ARM64_S11 = 111,
            CV_ARM64_S12 = 112,
            CV_ARM64_S13 = 113,
            CV_ARM64_S14 = 114,
            CV_ARM64_S15 = 115,
            CV_ARM64_S16 = 116,
            CV_ARM64_S17 = 117,
            CV_ARM64_S18 = 118,
            CV_ARM64_S19 = 119,
            CV_ARM64_S20 = 120,
            CV_ARM64_S21 = 121,
            CV_ARM64_S22 = 122,
            CV_ARM64_S23 = 123,
            CV_ARM64_S24 = 124,
            CV_ARM64_S25 = 125,
            CV_ARM64_S26 = 126,
            CV_ARM64_S27 = 127,
            CV_ARM64_S28 = 128,
            CV_ARM64_S29 = 129,
            CV_ARM64_S30 = 130,
            CV_ARM64_S31 = 131,

            // 64-bit floating point registers

            CV_ARM64_D0 = 140,
            CV_ARM64_D1 = 141,
            CV_ARM64_D2 = 142,
            CV_ARM64_D3 = 143,
            CV_ARM64_D4 = 144,
            CV_ARM64_D5 = 145,
            CV_ARM64_D6 = 146,
            CV_ARM64_D7 = 147,
            CV_ARM64_D8 = 148,
            CV_ARM64_D9 = 149,
            CV_ARM64_D10 = 150,
            CV_ARM64_D11 = 151,
            CV_ARM64_D12 = 152,
            CV_ARM64_D13 = 153,
            CV_ARM64_D14 = 154,
            CV_ARM64_D15 = 155,
            CV_ARM64_D16 = 156,
            CV_ARM64_D17 = 157,
            CV_ARM64_D18 = 158,
            CV_ARM64_D19 = 159,
            CV_ARM64_D20 = 160,
            CV_ARM64_D21 = 161,
            CV_ARM64_D22 = 162,
            CV_ARM64_D23 = 163,
            CV_ARM64_D24 = 164,
            CV_ARM64_D25 = 165,
            CV_ARM64_D26 = 166,
            CV_ARM64_D27 = 167,
            CV_ARM64_D28 = 168,
            CV_ARM64_D29 = 169,
            CV_ARM64_D30 = 170,
            CV_ARM64_D31 = 171,

            // 128-bit SIMD registers

            CV_ARM64_Q0 = 180,
            CV_ARM64_Q1 = 181,
            CV_ARM64_Q2 = 182,
            CV_ARM64_Q3 = 183,
            CV_ARM64_Q4 = 184,
            CV_ARM64_Q5 = 185,
            CV_ARM64_Q6 = 186,
            CV_ARM64_Q7 = 187,
            CV_ARM64_Q8 = 188,
            CV_ARM64_Q9 = 189,
            CV_ARM64_Q10 = 190,
            CV_ARM64_Q11 = 191,
            CV_ARM64_Q12 = 192,
            CV_ARM64_Q13 = 193,
            CV_ARM64_Q14 = 194,
            CV_ARM64_Q15 = 195,
            CV_ARM64_Q16 = 196,
            CV_ARM64_Q17 = 197,
            CV_ARM64_Q18 = 198,
            CV_ARM64_Q19 = 199,
            CV_ARM64_Q20 = 200,
            CV_ARM64_Q21 = 201,
            CV_ARM64_Q22 = 202,
            CV_ARM64_Q23 = 203,
            CV_ARM64_Q24 = 204,
            CV_ARM64_Q25 = 205,
            CV_ARM64_Q26 = 206,
            CV_ARM64_Q27 = 207,
            CV_ARM64_Q28 = 208,
            CV_ARM64_Q29 = 209,
            CV_ARM64_Q30 = 210,
            CV_ARM64_Q31 = 211,

            // Floating point status register

            CV_ARM64_FPSR = 220,

            //
            // Register set for Intel IA64
            //

            CV_IA64_NOREG = CV_REG_NONE,

            // Branch Registers

            CV_IA64_Br0 = 512,
            CV_IA64_Br1 = 513,
            CV_IA64_Br2 = 514,
            CV_IA64_Br3 = 515,
            CV_IA64_Br4 = 516,
            CV_IA64_Br5 = 517,
            CV_IA64_Br6 = 518,
            CV_IA64_Br7 = 519,

            // Predicate Registers

            CV_IA64_P0 = 704,
            CV_IA64_P1 = 705,
            CV_IA64_P2 = 706,
            CV_IA64_P3 = 707,
            CV_IA64_P4 = 708,
            CV_IA64_P5 = 709,
            CV_IA64_P6 = 710,
            CV_IA64_P7 = 711,
            CV_IA64_P8 = 712,
            CV_IA64_P9 = 713,
            CV_IA64_P10 = 714,
            CV_IA64_P11 = 715,
            CV_IA64_P12 = 716,
            CV_IA64_P13 = 717,
            CV_IA64_P14 = 718,
            CV_IA64_P15 = 719,
            CV_IA64_P16 = 720,
            CV_IA64_P17 = 721,
            CV_IA64_P18 = 722,
            CV_IA64_P19 = 723,
            CV_IA64_P20 = 724,
            CV_IA64_P21 = 725,
            CV_IA64_P22 = 726,
            CV_IA64_P23 = 727,
            CV_IA64_P24 = 728,
            CV_IA64_P25 = 729,
            CV_IA64_P26 = 730,
            CV_IA64_P27 = 731,
            CV_IA64_P28 = 732,
            CV_IA64_P29 = 733,
            CV_IA64_P30 = 734,
            CV_IA64_P31 = 735,
            CV_IA64_P32 = 736,
            CV_IA64_P33 = 737,
            CV_IA64_P34 = 738,
            CV_IA64_P35 = 739,
            CV_IA64_P36 = 740,
            CV_IA64_P37 = 741,
            CV_IA64_P38 = 742,
            CV_IA64_P39 = 743,
            CV_IA64_P40 = 744,
            CV_IA64_P41 = 745,
            CV_IA64_P42 = 746,
            CV_IA64_P43 = 747,
            CV_IA64_P44 = 748,
            CV_IA64_P45 = 749,
            CV_IA64_P46 = 750,
            CV_IA64_P47 = 751,
            CV_IA64_P48 = 752,
            CV_IA64_P49 = 753,
            CV_IA64_P50 = 754,
            CV_IA64_P51 = 755,
            CV_IA64_P52 = 756,
            CV_IA64_P53 = 757,
            CV_IA64_P54 = 758,
            CV_IA64_P55 = 759,
            CV_IA64_P56 = 760,
            CV_IA64_P57 = 761,
            CV_IA64_P58 = 762,
            CV_IA64_P59 = 763,
            CV_IA64_P60 = 764,
            CV_IA64_P61 = 765,
            CV_IA64_P62 = 766,
            CV_IA64_P63 = 767,

            CV_IA64_Preds = 768,

            // Banked General Registers

            CV_IA64_IntH0 = 832,
            CV_IA64_IntH1 = 833,
            CV_IA64_IntH2 = 834,
            CV_IA64_IntH3 = 835,
            CV_IA64_IntH4 = 836,
            CV_IA64_IntH5 = 837,
            CV_IA64_IntH6 = 838,
            CV_IA64_IntH7 = 839,
            CV_IA64_IntH8 = 840,
            CV_IA64_IntH9 = 841,
            CV_IA64_IntH10 = 842,
            CV_IA64_IntH11 = 843,
            CV_IA64_IntH12 = 844,
            CV_IA64_IntH13 = 845,
            CV_IA64_IntH14 = 846,
            CV_IA64_IntH15 = 847,

            // Special Registers

            CV_IA64_Ip = 1016,
            CV_IA64_Umask = 1017,
            CV_IA64_Cfm = 1018,
            CV_IA64_Psr = 1019,

            // Banked General Registers

            CV_IA64_Nats = 1020,
            CV_IA64_Nats2 = 1021,
            CV_IA64_Nats3 = 1022,

            // General-Purpose Registers

            // Integer registers
            CV_IA64_IntR0 = 1024,
            CV_IA64_IntR1 = 1025,
            CV_IA64_IntR2 = 1026,
            CV_IA64_IntR3 = 1027,
            CV_IA64_IntR4 = 1028,
            CV_IA64_IntR5 = 1029,
            CV_IA64_IntR6 = 1030,
            CV_IA64_IntR7 = 1031,
            CV_IA64_IntR8 = 1032,
            CV_IA64_IntR9 = 1033,
            CV_IA64_IntR10 = 1034,
            CV_IA64_IntR11 = 1035,
            CV_IA64_IntR12 = 1036,
            CV_IA64_IntR13 = 1037,
            CV_IA64_IntR14 = 1038,
            CV_IA64_IntR15 = 1039,
            CV_IA64_IntR16 = 1040,
            CV_IA64_IntR17 = 1041,
            CV_IA64_IntR18 = 1042,
            CV_IA64_IntR19 = 1043,
            CV_IA64_IntR20 = 1044,
            CV_IA64_IntR21 = 1045,
            CV_IA64_IntR22 = 1046,
            CV_IA64_IntR23 = 1047,
            CV_IA64_IntR24 = 1048,
            CV_IA64_IntR25 = 1049,
            CV_IA64_IntR26 = 1050,
            CV_IA64_IntR27 = 1051,
            CV_IA64_IntR28 = 1052,
            CV_IA64_IntR29 = 1053,
            CV_IA64_IntR30 = 1054,
            CV_IA64_IntR31 = 1055,

            // Register Stack
            CV_IA64_IntR32 = 1056,
            CV_IA64_IntR33 = 1057,
            CV_IA64_IntR34 = 1058,
            CV_IA64_IntR35 = 1059,
            CV_IA64_IntR36 = 1060,
            CV_IA64_IntR37 = 1061,
            CV_IA64_IntR38 = 1062,
            CV_IA64_IntR39 = 1063,
            CV_IA64_IntR40 = 1064,
            CV_IA64_IntR41 = 1065,
            CV_IA64_IntR42 = 1066,
            CV_IA64_IntR43 = 1067,
            CV_IA64_IntR44 = 1068,
            CV_IA64_IntR45 = 1069,
            CV_IA64_IntR46 = 1070,
            CV_IA64_IntR47 = 1071,
            CV_IA64_IntR48 = 1072,
            CV_IA64_IntR49 = 1073,
            CV_IA64_IntR50 = 1074,
            CV_IA64_IntR51 = 1075,
            CV_IA64_IntR52 = 1076,
            CV_IA64_IntR53 = 1077,
            CV_IA64_IntR54 = 1078,
            CV_IA64_IntR55 = 1079,
            CV_IA64_IntR56 = 1080,
            CV_IA64_IntR57 = 1081,
            CV_IA64_IntR58 = 1082,
            CV_IA64_IntR59 = 1083,
            CV_IA64_IntR60 = 1084,
            CV_IA64_IntR61 = 1085,
            CV_IA64_IntR62 = 1086,
            CV_IA64_IntR63 = 1087,
            CV_IA64_IntR64 = 1088,
            CV_IA64_IntR65 = 1089,
            CV_IA64_IntR66 = 1090,
            CV_IA64_IntR67 = 1091,
            CV_IA64_IntR68 = 1092,
            CV_IA64_IntR69 = 1093,
            CV_IA64_IntR70 = 1094,
            CV_IA64_IntR71 = 1095,
            CV_IA64_IntR72 = 1096,
            CV_IA64_IntR73 = 1097,
            CV_IA64_IntR74 = 1098,
            CV_IA64_IntR75 = 1099,
            CV_IA64_IntR76 = 1100,
            CV_IA64_IntR77 = 1101,
            CV_IA64_IntR78 = 1102,
            CV_IA64_IntR79 = 1103,
            CV_IA64_IntR80 = 1104,
            CV_IA64_IntR81 = 1105,
            CV_IA64_IntR82 = 1106,
            CV_IA64_IntR83 = 1107,
            CV_IA64_IntR84 = 1108,
            CV_IA64_IntR85 = 1109,
            CV_IA64_IntR86 = 1110,
            CV_IA64_IntR87 = 1111,
            CV_IA64_IntR88 = 1112,
            CV_IA64_IntR89 = 1113,
            CV_IA64_IntR90 = 1114,
            CV_IA64_IntR91 = 1115,
            CV_IA64_IntR92 = 1116,
            CV_IA64_IntR93 = 1117,
            CV_IA64_IntR94 = 1118,
            CV_IA64_IntR95 = 1119,
            CV_IA64_IntR96 = 1120,
            CV_IA64_IntR97 = 1121,
            CV_IA64_IntR98 = 1122,
            CV_IA64_IntR99 = 1123,
            CV_IA64_IntR100 = 1124,
            CV_IA64_IntR101 = 1125,
            CV_IA64_IntR102 = 1126,
            CV_IA64_IntR103 = 1127,
            CV_IA64_IntR104 = 1128,
            CV_IA64_IntR105 = 1129,
            CV_IA64_IntR106 = 1130,
            CV_IA64_IntR107 = 1131,
            CV_IA64_IntR108 = 1132,
            CV_IA64_IntR109 = 1133,
            CV_IA64_IntR110 = 1134,
            CV_IA64_IntR111 = 1135,
            CV_IA64_IntR112 = 1136,
            CV_IA64_IntR113 = 1137,
            CV_IA64_IntR114 = 1138,
            CV_IA64_IntR115 = 1139,
            CV_IA64_IntR116 = 1140,
            CV_IA64_IntR117 = 1141,
            CV_IA64_IntR118 = 1142,
            CV_IA64_IntR119 = 1143,
            CV_IA64_IntR120 = 1144,
            CV_IA64_IntR121 = 1145,
            CV_IA64_IntR122 = 1146,
            CV_IA64_IntR123 = 1147,
            CV_IA64_IntR124 = 1148,
            CV_IA64_IntR125 = 1149,
            CV_IA64_IntR126 = 1150,
            CV_IA64_IntR127 = 1151,

            // Floating-Point Registers

            // Low Floating Point Registers
            CV_IA64_FltF0 = 2048,
            CV_IA64_FltF1 = 2049,
            CV_IA64_FltF2 = 2050,
            CV_IA64_FltF3 = 2051,
            CV_IA64_FltF4 = 2052,
            CV_IA64_FltF5 = 2053,
            CV_IA64_FltF6 = 2054,
            CV_IA64_FltF7 = 2055,
            CV_IA64_FltF8 = 2056,
            CV_IA64_FltF9 = 2057,
            CV_IA64_FltF10 = 2058,
            CV_IA64_FltF11 = 2059,
            CV_IA64_FltF12 = 2060,
            CV_IA64_FltF13 = 2061,
            CV_IA64_FltF14 = 2062,
            CV_IA64_FltF15 = 2063,
            CV_IA64_FltF16 = 2064,
            CV_IA64_FltF17 = 2065,
            CV_IA64_FltF18 = 2066,
            CV_IA64_FltF19 = 2067,
            CV_IA64_FltF20 = 2068,
            CV_IA64_FltF21 = 2069,
            CV_IA64_FltF22 = 2070,
            CV_IA64_FltF23 = 2071,
            CV_IA64_FltF24 = 2072,
            CV_IA64_FltF25 = 2073,
            CV_IA64_FltF26 = 2074,
            CV_IA64_FltF27 = 2075,
            CV_IA64_FltF28 = 2076,
            CV_IA64_FltF29 = 2077,
            CV_IA64_FltF30 = 2078,
            CV_IA64_FltF31 = 2079,

            // High Floating Point Registers
            CV_IA64_FltF32 = 2080,
            CV_IA64_FltF33 = 2081,
            CV_IA64_FltF34 = 2082,
            CV_IA64_FltF35 = 2083,
            CV_IA64_FltF36 = 2084,
            CV_IA64_FltF37 = 2085,
            CV_IA64_FltF38 = 2086,
            CV_IA64_FltF39 = 2087,
            CV_IA64_FltF40 = 2088,
            CV_IA64_FltF41 = 2089,
            CV_IA64_FltF42 = 2090,
            CV_IA64_FltF43 = 2091,
            CV_IA64_FltF44 = 2092,
            CV_IA64_FltF45 = 2093,
            CV_IA64_FltF46 = 2094,
            CV_IA64_FltF47 = 2095,
            CV_IA64_FltF48 = 2096,
            CV_IA64_FltF49 = 2097,
            CV_IA64_FltF50 = 2098,
            CV_IA64_FltF51 = 2099,
            CV_IA64_FltF52 = 2100,
            CV_IA64_FltF53 = 2101,
            CV_IA64_FltF54 = 2102,
            CV_IA64_FltF55 = 2103,
            CV_IA64_FltF56 = 2104,
            CV_IA64_FltF57 = 2105,
            CV_IA64_FltF58 = 2106,
            CV_IA64_FltF59 = 2107,
            CV_IA64_FltF60 = 2108,
            CV_IA64_FltF61 = 2109,
            CV_IA64_FltF62 = 2110,
            CV_IA64_FltF63 = 2111,
            CV_IA64_FltF64 = 2112,
            CV_IA64_FltF65 = 2113,
            CV_IA64_FltF66 = 2114,
            CV_IA64_FltF67 = 2115,
            CV_IA64_FltF68 = 2116,
            CV_IA64_FltF69 = 2117,
            CV_IA64_FltF70 = 2118,
            CV_IA64_FltF71 = 2119,
            CV_IA64_FltF72 = 2120,
            CV_IA64_FltF73 = 2121,
            CV_IA64_FltF74 = 2122,
            CV_IA64_FltF75 = 2123,
            CV_IA64_FltF76 = 2124,
            CV_IA64_FltF77 = 2125,
            CV_IA64_FltF78 = 2126,
            CV_IA64_FltF79 = 2127,
            CV_IA64_FltF80 = 2128,
            CV_IA64_FltF81 = 2129,
            CV_IA64_FltF82 = 2130,
            CV_IA64_FltF83 = 2131,
            CV_IA64_FltF84 = 2132,
            CV_IA64_FltF85 = 2133,
            CV_IA64_FltF86 = 2134,
            CV_IA64_FltF87 = 2135,
            CV_IA64_FltF88 = 2136,
            CV_IA64_FltF89 = 2137,
            CV_IA64_FltF90 = 2138,
            CV_IA64_FltF91 = 2139,
            CV_IA64_FltF92 = 2140,
            CV_IA64_FltF93 = 2141,
            CV_IA64_FltF94 = 2142,
            CV_IA64_FltF95 = 2143,
            CV_IA64_FltF96 = 2144,
            CV_IA64_FltF97 = 2145,
            CV_IA64_FltF98 = 2146,
            CV_IA64_FltF99 = 2147,
            CV_IA64_FltF100 = 2148,
            CV_IA64_FltF101 = 2149,
            CV_IA64_FltF102 = 2150,
            CV_IA64_FltF103 = 2151,
            CV_IA64_FltF104 = 2152,
            CV_IA64_FltF105 = 2153,
            CV_IA64_FltF106 = 2154,
            CV_IA64_FltF107 = 2155,
            CV_IA64_FltF108 = 2156,
            CV_IA64_FltF109 = 2157,
            CV_IA64_FltF110 = 2158,
            CV_IA64_FltF111 = 2159,
            CV_IA64_FltF112 = 2160,
            CV_IA64_FltF113 = 2161,
            CV_IA64_FltF114 = 2162,
            CV_IA64_FltF115 = 2163,
            CV_IA64_FltF116 = 2164,
            CV_IA64_FltF117 = 2165,
            CV_IA64_FltF118 = 2166,
            CV_IA64_FltF119 = 2167,
            CV_IA64_FltF120 = 2168,
            CV_IA64_FltF121 = 2169,
            CV_IA64_FltF122 = 2170,
            CV_IA64_FltF123 = 2171,
            CV_IA64_FltF124 = 2172,
            CV_IA64_FltF125 = 2173,
            CV_IA64_FltF126 = 2174,
            CV_IA64_FltF127 = 2175,

            // Application Registers

            CV_IA64_ApKR0 = 3072,
            CV_IA64_ApKR1 = 3073,
            CV_IA64_ApKR2 = 3074,
            CV_IA64_ApKR3 = 3075,
            CV_IA64_ApKR4 = 3076,
            CV_IA64_ApKR5 = 3077,
            CV_IA64_ApKR6 = 3078,
            CV_IA64_ApKR7 = 3079,
            CV_IA64_AR8 = 3080,
            CV_IA64_AR9 = 3081,
            CV_IA64_AR10 = 3082,
            CV_IA64_AR11 = 3083,
            CV_IA64_AR12 = 3084,
            CV_IA64_AR13 = 3085,
            CV_IA64_AR14 = 3086,
            CV_IA64_AR15 = 3087,
            CV_IA64_RsRSC = 3088,
            CV_IA64_RsBSP = 3089,
            CV_IA64_RsBSPSTORE = 3090,
            CV_IA64_RsRNAT = 3091,
            CV_IA64_AR20 = 3092,
            CV_IA64_StFCR = 3093,
            CV_IA64_AR22 = 3094,
            CV_IA64_AR23 = 3095,
            CV_IA64_EFLAG = 3096,
            CV_IA64_CSD = 3097,
            CV_IA64_SSD = 3098,
            CV_IA64_CFLG = 3099,
            CV_IA64_StFSR = 3100,
            CV_IA64_StFIR = 3101,
            CV_IA64_StFDR = 3102,
            CV_IA64_AR31 = 3103,
            CV_IA64_ApCCV = 3104,
            CV_IA64_AR33 = 3105,
            CV_IA64_AR34 = 3106,
            CV_IA64_AR35 = 3107,
            CV_IA64_ApUNAT = 3108,
            CV_IA64_AR37 = 3109,
            CV_IA64_AR38 = 3110,
            CV_IA64_AR39 = 3111,
            CV_IA64_StFPSR = 3112,
            CV_IA64_AR41 = 3113,
            CV_IA64_AR42 = 3114,
            CV_IA64_AR43 = 3115,
            CV_IA64_ApITC = 3116,
            CV_IA64_AR45 = 3117,
            CV_IA64_AR46 = 3118,
            CV_IA64_AR47 = 3119,
            CV_IA64_AR48 = 3120,
            CV_IA64_AR49 = 3121,
            CV_IA64_AR50 = 3122,
            CV_IA64_AR51 = 3123,
            CV_IA64_AR52 = 3124,
            CV_IA64_AR53 = 3125,
            CV_IA64_AR54 = 3126,
            CV_IA64_AR55 = 3127,
            CV_IA64_AR56 = 3128,
            CV_IA64_AR57 = 3129,
            CV_IA64_AR58 = 3130,
            CV_IA64_AR59 = 3131,
            CV_IA64_AR60 = 3132,
            CV_IA64_AR61 = 3133,
            CV_IA64_AR62 = 3134,
            CV_IA64_AR63 = 3135,
            CV_IA64_RsPFS = 3136,
            CV_IA64_ApLC = 3137,
            CV_IA64_ApEC = 3138,
            CV_IA64_AR67 = 3139,
            CV_IA64_AR68 = 3140,
            CV_IA64_AR69 = 3141,
            CV_IA64_AR70 = 3142,
            CV_IA64_AR71 = 3143,
            CV_IA64_AR72 = 3144,
            CV_IA64_AR73 = 3145,
            CV_IA64_AR74 = 3146,
            CV_IA64_AR75 = 3147,
            CV_IA64_AR76 = 3148,
            CV_IA64_AR77 = 3149,
            CV_IA64_AR78 = 3150,
            CV_IA64_AR79 = 3151,
            CV_IA64_AR80 = 3152,
            CV_IA64_AR81 = 3153,
            CV_IA64_AR82 = 3154,
            CV_IA64_AR83 = 3155,
            CV_IA64_AR84 = 3156,
            CV_IA64_AR85 = 3157,
            CV_IA64_AR86 = 3158,
            CV_IA64_AR87 = 3159,
            CV_IA64_AR88 = 3160,
            CV_IA64_AR89 = 3161,
            CV_IA64_AR90 = 3162,
            CV_IA64_AR91 = 3163,
            CV_IA64_AR92 = 3164,
            CV_IA64_AR93 = 3165,
            CV_IA64_AR94 = 3166,
            CV_IA64_AR95 = 3167,
            CV_IA64_AR96 = 3168,
            CV_IA64_AR97 = 3169,
            CV_IA64_AR98 = 3170,
            CV_IA64_AR99 = 3171,
            CV_IA64_AR100 = 3172,
            CV_IA64_AR101 = 3173,
            CV_IA64_AR102 = 3174,
            CV_IA64_AR103 = 3175,
            CV_IA64_AR104 = 3176,
            CV_IA64_AR105 = 3177,
            CV_IA64_AR106 = 3178,
            CV_IA64_AR107 = 3179,
            CV_IA64_AR108 = 3180,
            CV_IA64_AR109 = 3181,
            CV_IA64_AR110 = 3182,
            CV_IA64_AR111 = 3183,
            CV_IA64_AR112 = 3184,
            CV_IA64_AR113 = 3185,
            CV_IA64_AR114 = 3186,
            CV_IA64_AR115 = 3187,
            CV_IA64_AR116 = 3188,
            CV_IA64_AR117 = 3189,
            CV_IA64_AR118 = 3190,
            CV_IA64_AR119 = 3191,
            CV_IA64_AR120 = 3192,
            CV_IA64_AR121 = 3193,
            CV_IA64_AR122 = 3194,
            CV_IA64_AR123 = 3195,
            CV_IA64_AR124 = 3196,
            CV_IA64_AR125 = 3197,
            CV_IA64_AR126 = 3198,
            CV_IA64_AR127 = 3199,

            // CPUID Registers

            CV_IA64_CPUID0 = 3328,
            CV_IA64_CPUID1 = 3329,
            CV_IA64_CPUID2 = 3330,
            CV_IA64_CPUID3 = 3331,
            CV_IA64_CPUID4 = 3332,

            // Control Registers

            CV_IA64_ApDCR = 4096,
            CV_IA64_ApITM = 4097,
            CV_IA64_ApIVA = 4098,
            CV_IA64_CR3 = 4099,
            CV_IA64_CR4 = 4100,
            CV_IA64_CR5 = 4101,
            CV_IA64_CR6 = 4102,
            CV_IA64_CR7 = 4103,
            CV_IA64_ApPTA = 4104,
            CV_IA64_ApGPTA = 4105,
            CV_IA64_CR10 = 4106,
            CV_IA64_CR11 = 4107,
            CV_IA64_CR12 = 4108,
            CV_IA64_CR13 = 4109,
            CV_IA64_CR14 = 4110,
            CV_IA64_CR15 = 4111,
            CV_IA64_StIPSR = 4112,
            CV_IA64_StISR = 4113,
            CV_IA64_CR18 = 4114,
            CV_IA64_StIIP = 4115,
            CV_IA64_StIFA = 4116,
            CV_IA64_StITIR = 4117,
            CV_IA64_StIIPA = 4118,
            CV_IA64_StIFS = 4119,
            CV_IA64_StIIM = 4120,
            CV_IA64_StIHA = 4121,
            CV_IA64_CR26 = 4122,
            CV_IA64_CR27 = 4123,
            CV_IA64_CR28 = 4124,
            CV_IA64_CR29 = 4125,
            CV_IA64_CR30 = 4126,
            CV_IA64_CR31 = 4127,
            CV_IA64_CR32 = 4128,
            CV_IA64_CR33 = 4129,
            CV_IA64_CR34 = 4130,
            CV_IA64_CR35 = 4131,
            CV_IA64_CR36 = 4132,
            CV_IA64_CR37 = 4133,
            CV_IA64_CR38 = 4134,
            CV_IA64_CR39 = 4135,
            CV_IA64_CR40 = 4136,
            CV_IA64_CR41 = 4137,
            CV_IA64_CR42 = 4138,
            CV_IA64_CR43 = 4139,
            CV_IA64_CR44 = 4140,
            CV_IA64_CR45 = 4141,
            CV_IA64_CR46 = 4142,
            CV_IA64_CR47 = 4143,
            CV_IA64_CR48 = 4144,
            CV_IA64_CR49 = 4145,
            CV_IA64_CR50 = 4146,
            CV_IA64_CR51 = 4147,
            CV_IA64_CR52 = 4148,
            CV_IA64_CR53 = 4149,
            CV_IA64_CR54 = 4150,
            CV_IA64_CR55 = 4151,
            CV_IA64_CR56 = 4152,
            CV_IA64_CR57 = 4153,
            CV_IA64_CR58 = 4154,
            CV_IA64_CR59 = 4155,
            CV_IA64_CR60 = 4156,
            CV_IA64_CR61 = 4157,
            CV_IA64_CR62 = 4158,
            CV_IA64_CR63 = 4159,
            CV_IA64_SaLID = 4160,
            CV_IA64_SaIVR = 4161,
            CV_IA64_SaTPR = 4162,
            CV_IA64_SaEOI = 4163,
            CV_IA64_SaIRR0 = 4164,
            CV_IA64_SaIRR1 = 4165,
            CV_IA64_SaIRR2 = 4166,
            CV_IA64_SaIRR3 = 4167,
            CV_IA64_SaITV = 4168,
            CV_IA64_SaPMV = 4169,
            CV_IA64_SaCMCV = 4170,
            CV_IA64_CR75 = 4171,
            CV_IA64_CR76 = 4172,
            CV_IA64_CR77 = 4173,
            CV_IA64_CR78 = 4174,
            CV_IA64_CR79 = 4175,
            CV_IA64_SaLRR0 = 4176,
            CV_IA64_SaLRR1 = 4177,
            CV_IA64_CR82 = 4178,
            CV_IA64_CR83 = 4179,
            CV_IA64_CR84 = 4180,
            CV_IA64_CR85 = 4181,
            CV_IA64_CR86 = 4182,
            CV_IA64_CR87 = 4183,
            CV_IA64_CR88 = 4184,
            CV_IA64_CR89 = 4185,
            CV_IA64_CR90 = 4186,
            CV_IA64_CR91 = 4187,
            CV_IA64_CR92 = 4188,
            CV_IA64_CR93 = 4189,
            CV_IA64_CR94 = 4190,
            CV_IA64_CR95 = 4191,
            CV_IA64_CR96 = 4192,
            CV_IA64_CR97 = 4193,
            CV_IA64_CR98 = 4194,
            CV_IA64_CR99 = 4195,
            CV_IA64_CR100 = 4196,
            CV_IA64_CR101 = 4197,
            CV_IA64_CR102 = 4198,
            CV_IA64_CR103 = 4199,
            CV_IA64_CR104 = 4200,
            CV_IA64_CR105 = 4201,
            CV_IA64_CR106 = 4202,
            CV_IA64_CR107 = 4203,
            CV_IA64_CR108 = 4204,
            CV_IA64_CR109 = 4205,
            CV_IA64_CR110 = 4206,
            CV_IA64_CR111 = 4207,
            CV_IA64_CR112 = 4208,
            CV_IA64_CR113 = 4209,
            CV_IA64_CR114 = 4210,
            CV_IA64_CR115 = 4211,
            CV_IA64_CR116 = 4212,
            CV_IA64_CR117 = 4213,
            CV_IA64_CR118 = 4214,
            CV_IA64_CR119 = 4215,
            CV_IA64_CR120 = 4216,
            CV_IA64_CR121 = 4217,
            CV_IA64_CR122 = 4218,
            CV_IA64_CR123 = 4219,
            CV_IA64_CR124 = 4220,
            CV_IA64_CR125 = 4221,
            CV_IA64_CR126 = 4222,
            CV_IA64_CR127 = 4223,

            // Protection Key Registers

            CV_IA64_Pkr0 = 5120,
            CV_IA64_Pkr1 = 5121,
            CV_IA64_Pkr2 = 5122,
            CV_IA64_Pkr3 = 5123,
            CV_IA64_Pkr4 = 5124,
            CV_IA64_Pkr5 = 5125,
            CV_IA64_Pkr6 = 5126,
            CV_IA64_Pkr7 = 5127,
            CV_IA64_Pkr8 = 5128,
            CV_IA64_Pkr9 = 5129,
            CV_IA64_Pkr10 = 5130,
            CV_IA64_Pkr11 = 5131,
            CV_IA64_Pkr12 = 5132,
            CV_IA64_Pkr13 = 5133,
            CV_IA64_Pkr14 = 5134,
            CV_IA64_Pkr15 = 5135,

            // Region Registers

            CV_IA64_Rr0 = 6144,
            CV_IA64_Rr1 = 6145,
            CV_IA64_Rr2 = 6146,
            CV_IA64_Rr3 = 6147,
            CV_IA64_Rr4 = 6148,
            CV_IA64_Rr5 = 6149,
            CV_IA64_Rr6 = 6150,
            CV_IA64_Rr7 = 6151,

            // Performance Monitor Data Registers

            CV_IA64_PFD0 = 7168,
            CV_IA64_PFD1 = 7169,
            CV_IA64_PFD2 = 7170,
            CV_IA64_PFD3 = 7171,
            CV_IA64_PFD4 = 7172,
            CV_IA64_PFD5 = 7173,
            CV_IA64_PFD6 = 7174,
            CV_IA64_PFD7 = 7175,
            CV_IA64_PFD8 = 7176,
            CV_IA64_PFD9 = 7177,
            CV_IA64_PFD10 = 7178,
            CV_IA64_PFD11 = 7179,
            CV_IA64_PFD12 = 7180,
            CV_IA64_PFD13 = 7181,
            CV_IA64_PFD14 = 7182,
            CV_IA64_PFD15 = 7183,
            CV_IA64_PFD16 = 7184,
            CV_IA64_PFD17 = 7185,

            // Performance Monitor Config Registers

            CV_IA64_PFC0 = 7424,
            CV_IA64_PFC1 = 7425,
            CV_IA64_PFC2 = 7426,
            CV_IA64_PFC3 = 7427,
            CV_IA64_PFC4 = 7428,
            CV_IA64_PFC5 = 7429,
            CV_IA64_PFC6 = 7430,
            CV_IA64_PFC7 = 7431,
            CV_IA64_PFC8 = 7432,
            CV_IA64_PFC9 = 7433,
            CV_IA64_PFC10 = 7434,
            CV_IA64_PFC11 = 7435,
            CV_IA64_PFC12 = 7436,
            CV_IA64_PFC13 = 7437,
            CV_IA64_PFC14 = 7438,
            CV_IA64_PFC15 = 7439,

            // Instruction Translation Registers

            CV_IA64_TrI0 = 8192,
            CV_IA64_TrI1 = 8193,
            CV_IA64_TrI2 = 8194,
            CV_IA64_TrI3 = 8195,
            CV_IA64_TrI4 = 8196,
            CV_IA64_TrI5 = 8197,
            CV_IA64_TrI6 = 8198,
            CV_IA64_TrI7 = 8199,

            // Data Translation Registers

            CV_IA64_TrD0 = 8320,
            CV_IA64_TrD1 = 8321,
            CV_IA64_TrD2 = 8322,
            CV_IA64_TrD3 = 8323,
            CV_IA64_TrD4 = 8324,
            CV_IA64_TrD5 = 8325,
            CV_IA64_TrD6 = 8326,
            CV_IA64_TrD7 = 8327,

            // Instruction Breakpoint Registers

            CV_IA64_DbI0 = 8448,
            CV_IA64_DbI1 = 8449,
            CV_IA64_DbI2 = 8450,
            CV_IA64_DbI3 = 8451,
            CV_IA64_DbI4 = 8452,
            CV_IA64_DbI5 = 8453,
            CV_IA64_DbI6 = 8454,
            CV_IA64_DbI7 = 8455,

            // Data Breakpoint Registers

            CV_IA64_DbD0 = 8576,
            CV_IA64_DbD1 = 8577,
            CV_IA64_DbD2 = 8578,
            CV_IA64_DbD3 = 8579,
            CV_IA64_DbD4 = 8580,
            CV_IA64_DbD5 = 8581,
            CV_IA64_DbD6 = 8582,
            CV_IA64_DbD7 = 8583,

            //
            // Register set for the TriCore processor.
            //

            CV_TRI_NOREG = CV_REG_NONE,

            // General Purpose Data Registers

            CV_TRI_D0 = 10,
            CV_TRI_D1 = 11,
            CV_TRI_D2 = 12,
            CV_TRI_D3 = 13,
            CV_TRI_D4 = 14,
            CV_TRI_D5 = 15,
            CV_TRI_D6 = 16,
            CV_TRI_D7 = 17,
            CV_TRI_D8 = 18,
            CV_TRI_D9 = 19,
            CV_TRI_D10 = 20,
            CV_TRI_D11 = 21,
            CV_TRI_D12 = 22,
            CV_TRI_D13 = 23,
            CV_TRI_D14 = 24,
            CV_TRI_D15 = 25,

            // General Purpose Address Registers

            CV_TRI_A0 = 26,
            CV_TRI_A1 = 27,
            CV_TRI_A2 = 28,
            CV_TRI_A3 = 29,
            CV_TRI_A4 = 30,
            CV_TRI_A5 = 31,
            CV_TRI_A6 = 32,
            CV_TRI_A7 = 33,
            CV_TRI_A8 = 34,
            CV_TRI_A9 = 35,
            CV_TRI_A10 = 36,
            CV_TRI_A11 = 37,
            CV_TRI_A12 = 38,
            CV_TRI_A13 = 39,
            CV_TRI_A14 = 40,
            CV_TRI_A15 = 41,

            // Extended (64-bit) data registers

            CV_TRI_E0 = 42,
            CV_TRI_E2 = 43,
            CV_TRI_E4 = 44,
            CV_TRI_E6 = 45,
            CV_TRI_E8 = 46,
            CV_TRI_E10 = 47,
            CV_TRI_E12 = 48,
            CV_TRI_E14 = 49,

            // Extended (64-bit) address registers

            CV_TRI_EA0 = 50,
            CV_TRI_EA2 = 51,
            CV_TRI_EA4 = 52,
            CV_TRI_EA6 = 53,
            CV_TRI_EA8 = 54,
            CV_TRI_EA10 = 55,
            CV_TRI_EA12 = 56,
            CV_TRI_EA14 = 57,

            CV_TRI_PSW = 58,
            CV_TRI_PCXI = 59,
            CV_TRI_PC = 60,
            CV_TRI_FCX = 61,
            CV_TRI_LCX = 62,
            CV_TRI_ISP = 63,
            CV_TRI_ICR = 64,
            CV_TRI_BIV = 65,
            CV_TRI_BTV = 66,
            CV_TRI_SYSCON = 67,
            CV_TRI_DPRx_0 = 68,
            CV_TRI_DPRx_1 = 69,
            CV_TRI_DPRx_2 = 70,
            CV_TRI_DPRx_3 = 71,
            CV_TRI_CPRx_0 = 68,
            CV_TRI_CPRx_1 = 69,
            CV_TRI_CPRx_2 = 70,
            CV_TRI_CPRx_3 = 71,
            CV_TRI_DPMx_0 = 68,
            CV_TRI_DPMx_1 = 69,
            CV_TRI_DPMx_2 = 70,
            CV_TRI_DPMx_3 = 71,
            CV_TRI_CPMx_0 = 68,
            CV_TRI_CPMx_1 = 69,
            CV_TRI_CPMx_2 = 70,
            CV_TRI_CPMx_3 = 71,
            CV_TRI_DBGSSR = 72,
            CV_TRI_EXEVT = 73,
            CV_TRI_SWEVT = 74,
            CV_TRI_CREVT = 75,
            CV_TRI_TRnEVT = 76,
            CV_TRI_MMUCON = 77,
            CV_TRI_ASI = 78,
            CV_TRI_TVA = 79,
            CV_TRI_TPA = 80,
            CV_TRI_TPX = 81,
            CV_TRI_TFA = 82,

            //
            // Register set for the AM33 and related processors.
            //

            CV_AM33_NOREG = CV_REG_NONE,

            // "Extended" (general purpose integer) registers
            CV_AM33_E0 = 10,
            CV_AM33_E1 = 11,
            CV_AM33_E2 = 12,
            CV_AM33_E3 = 13,
            CV_AM33_E4 = 14,
            CV_AM33_E5 = 15,
            CV_AM33_E6 = 16,
            CV_AM33_E7 = 17,

            // Address registers
            CV_AM33_A0 = 20,
            CV_AM33_A1 = 21,
            CV_AM33_A2 = 22,
            CV_AM33_A3 = 23,

            // Integer data registers
            CV_AM33_D0 = 30,
            CV_AM33_D1 = 31,
            CV_AM33_D2 = 32,
            CV_AM33_D3 = 33,

            // (Single-precision) floating-point registers
            CV_AM33_FS0 = 40,
            CV_AM33_FS1 = 41,
            CV_AM33_FS2 = 42,
            CV_AM33_FS3 = 43,
            CV_AM33_FS4 = 44,
            CV_AM33_FS5 = 45,
            CV_AM33_FS6 = 46,
            CV_AM33_FS7 = 47,
            CV_AM33_FS8 = 48,
            CV_AM33_FS9 = 49,
            CV_AM33_FS10 = 50,
            CV_AM33_FS11 = 51,
            CV_AM33_FS12 = 52,
            CV_AM33_FS13 = 53,
            CV_AM33_FS14 = 54,
            CV_AM33_FS15 = 55,
            CV_AM33_FS16 = 56,
            CV_AM33_FS17 = 57,
            CV_AM33_FS18 = 58,
            CV_AM33_FS19 = 59,
            CV_AM33_FS20 = 60,
            CV_AM33_FS21 = 61,
            CV_AM33_FS22 = 62,
            CV_AM33_FS23 = 63,
            CV_AM33_FS24 = 64,
            CV_AM33_FS25 = 65,
            CV_AM33_FS26 = 66,
            CV_AM33_FS27 = 67,
            CV_AM33_FS28 = 68,
            CV_AM33_FS29 = 69,
            CV_AM33_FS30 = 70,
            CV_AM33_FS31 = 71,

            // Special purpose registers

            // Stack pointer
            CV_AM33_SP = 80,

            // Program counter
            CV_AM33_PC = 81,

            // Multiply-divide/accumulate registers
            CV_AM33_MDR = 82,
            CV_AM33_MDRQ = 83,
            CV_AM33_MCRH = 84,
            CV_AM33_MCRL = 85,
            CV_AM33_MCVF = 86,

            // CPU status words
            CV_AM33_EPSW = 87,
            CV_AM33_FPCR = 88,

            // Loop buffer registers
            CV_AM33_LIR = 89,
            CV_AM33_LAR = 90,

            //
            // Register set for the Mitsubishi M32R
            //

            CV_M32R_NOREG = CV_REG_NONE,

            CV_M32R_R0 = 10,
            CV_M32R_R1 = 11,
            CV_M32R_R2 = 12,
            CV_M32R_R3 = 13,
            CV_M32R_R4 = 14,
            CV_M32R_R5 = 15,
            CV_M32R_R6 = 16,
            CV_M32R_R7 = 17,
            CV_M32R_R8 = 18,
            CV_M32R_R9 = 19,
            CV_M32R_R10 = 20,
            CV_M32R_R11 = 21,
            CV_M32R_R12 = 22,   // Gloabal Pointer, if used
            CV_M32R_R13 = 23,   // Frame Pointer, if allocated
            CV_M32R_R14 = 24,   // Link Register
            CV_M32R_R15 = 25,   // Stack Pointer
            CV_M32R_PSW = 26,   // Preocessor Status Register
            CV_M32R_CBR = 27,   // Condition Bit Register
            CV_M32R_SPI = 28,   // Interrupt Stack Pointer
            CV_M32R_SPU = 29,   // User Stack Pointer
            CV_M32R_SPO = 30,   // OS Stack Pointer
            CV_M32R_BPC = 31,   // Backup Program Counter
            CV_M32R_ACHI = 32,   // Accumulator High
            CV_M32R_ACLO = 33,   // Accumulator Low
            CV_M32R_PC = 34,   // Program Counter

            //
            // Register set for the SuperH SHMedia processor including compact
            // mode
            //

            // Integer - 64 bit general registers
            CV_SHMEDIA_NOREG = CV_REG_NONE,
            CV_SHMEDIA_R0 = 10,
            CV_SHMEDIA_R1 = 11,
            CV_SHMEDIA_R2 = 12,
            CV_SHMEDIA_R3 = 13,
            CV_SHMEDIA_R4 = 14,
            CV_SHMEDIA_R5 = 15,
            CV_SHMEDIA_R6 = 16,
            CV_SHMEDIA_R7 = 17,
            CV_SHMEDIA_R8 = 18,
            CV_SHMEDIA_R9 = 19,
            CV_SHMEDIA_R10 = 20,
            CV_SHMEDIA_R11 = 21,
            CV_SHMEDIA_R12 = 22,
            CV_SHMEDIA_R13 = 23,
            CV_SHMEDIA_R14 = 24,
            CV_SHMEDIA_R15 = 25,
            CV_SHMEDIA_R16 = 26,
            CV_SHMEDIA_R17 = 27,
            CV_SHMEDIA_R18 = 28,
            CV_SHMEDIA_R19 = 29,
            CV_SHMEDIA_R20 = 30,
            CV_SHMEDIA_R21 = 31,
            CV_SHMEDIA_R22 = 32,
            CV_SHMEDIA_R23 = 33,
            CV_SHMEDIA_R24 = 34,
            CV_SHMEDIA_R25 = 35,
            CV_SHMEDIA_R26 = 36,
            CV_SHMEDIA_R27 = 37,
            CV_SHMEDIA_R28 = 38,
            CV_SHMEDIA_R29 = 39,
            CV_SHMEDIA_R30 = 40,
            CV_SHMEDIA_R31 = 41,
            CV_SHMEDIA_R32 = 42,
            CV_SHMEDIA_R33 = 43,
            CV_SHMEDIA_R34 = 44,
            CV_SHMEDIA_R35 = 45,
            CV_SHMEDIA_R36 = 46,
            CV_SHMEDIA_R37 = 47,
            CV_SHMEDIA_R38 = 48,
            CV_SHMEDIA_R39 = 49,
            CV_SHMEDIA_R40 = 50,
            CV_SHMEDIA_R41 = 51,
            CV_SHMEDIA_R42 = 52,
            CV_SHMEDIA_R43 = 53,
            CV_SHMEDIA_R44 = 54,
            CV_SHMEDIA_R45 = 55,
            CV_SHMEDIA_R46 = 56,
            CV_SHMEDIA_R47 = 57,
            CV_SHMEDIA_R48 = 58,
            CV_SHMEDIA_R49 = 59,
            CV_SHMEDIA_R50 = 60,
            CV_SHMEDIA_R51 = 61,
            CV_SHMEDIA_R52 = 62,
            CV_SHMEDIA_R53 = 63,
            CV_SHMEDIA_R54 = 64,
            CV_SHMEDIA_R55 = 65,
            CV_SHMEDIA_R56 = 66,
            CV_SHMEDIA_R57 = 67,
            CV_SHMEDIA_R58 = 68,
            CV_SHMEDIA_R59 = 69,
            CV_SHMEDIA_R60 = 70,
            CV_SHMEDIA_R61 = 71,
            CV_SHMEDIA_R62 = 72,
            CV_SHMEDIA_R63 = 73,

            // Target Registers - 32 bit
            CV_SHMEDIA_TR0 = 74,
            CV_SHMEDIA_TR1 = 75,
            CV_SHMEDIA_TR2 = 76,
            CV_SHMEDIA_TR3 = 77,
            CV_SHMEDIA_TR4 = 78,
            CV_SHMEDIA_TR5 = 79,
            CV_SHMEDIA_TR6 = 80,
            CV_SHMEDIA_TR7 = 81,
            CV_SHMEDIA_TR8 = 82, // future-proof
            CV_SHMEDIA_TR9 = 83, // future-proof
            CV_SHMEDIA_TR10 = 84, // future-proof
            CV_SHMEDIA_TR11 = 85, // future-proof
            CV_SHMEDIA_TR12 = 86, // future-proof
            CV_SHMEDIA_TR13 = 87, // future-proof
            CV_SHMEDIA_TR14 = 88, // future-proof
            CV_SHMEDIA_TR15 = 89, // future-proof

            // Single - 32 bit fp registers
            CV_SHMEDIA_FR0 = 128,
            CV_SHMEDIA_FR1 = 129,
            CV_SHMEDIA_FR2 = 130,
            CV_SHMEDIA_FR3 = 131,
            CV_SHMEDIA_FR4 = 132,
            CV_SHMEDIA_FR5 = 133,
            CV_SHMEDIA_FR6 = 134,
            CV_SHMEDIA_FR7 = 135,
            CV_SHMEDIA_FR8 = 136,
            CV_SHMEDIA_FR9 = 137,
            CV_SHMEDIA_FR10 = 138,
            CV_SHMEDIA_FR11 = 139,
            CV_SHMEDIA_FR12 = 140,
            CV_SHMEDIA_FR13 = 141,
            CV_SHMEDIA_FR14 = 142,
            CV_SHMEDIA_FR15 = 143,
            CV_SHMEDIA_FR16 = 144,
            CV_SHMEDIA_FR17 = 145,
            CV_SHMEDIA_FR18 = 146,
            CV_SHMEDIA_FR19 = 147,
            CV_SHMEDIA_FR20 = 148,
            CV_SHMEDIA_FR21 = 149,
            CV_SHMEDIA_FR22 = 150,
            CV_SHMEDIA_FR23 = 151,
            CV_SHMEDIA_FR24 = 152,
            CV_SHMEDIA_FR25 = 153,
            CV_SHMEDIA_FR26 = 154,
            CV_SHMEDIA_FR27 = 155,
            CV_SHMEDIA_FR28 = 156,
            CV_SHMEDIA_FR29 = 157,
            CV_SHMEDIA_FR30 = 158,
            CV_SHMEDIA_FR31 = 159,
            CV_SHMEDIA_FR32 = 160,
            CV_SHMEDIA_FR33 = 161,
            CV_SHMEDIA_FR34 = 162,
            CV_SHMEDIA_FR35 = 163,
            CV_SHMEDIA_FR36 = 164,
            CV_SHMEDIA_FR37 = 165,
            CV_SHMEDIA_FR38 = 166,
            CV_SHMEDIA_FR39 = 167,
            CV_SHMEDIA_FR40 = 168,
            CV_SHMEDIA_FR41 = 169,
            CV_SHMEDIA_FR42 = 170,
            CV_SHMEDIA_FR43 = 171,
            CV_SHMEDIA_FR44 = 172,
            CV_SHMEDIA_FR45 = 173,
            CV_SHMEDIA_FR46 = 174,
            CV_SHMEDIA_FR47 = 175,
            CV_SHMEDIA_FR48 = 176,
            CV_SHMEDIA_FR49 = 177,
            CV_SHMEDIA_FR50 = 178,
            CV_SHMEDIA_FR51 = 179,
            CV_SHMEDIA_FR52 = 180,
            CV_SHMEDIA_FR53 = 181,
            CV_SHMEDIA_FR54 = 182,
            CV_SHMEDIA_FR55 = 183,
            CV_SHMEDIA_FR56 = 184,
            CV_SHMEDIA_FR57 = 185,
            CV_SHMEDIA_FR58 = 186,
            CV_SHMEDIA_FR59 = 187,
            CV_SHMEDIA_FR60 = 188,
            CV_SHMEDIA_FR61 = 189,
            CV_SHMEDIA_FR62 = 190,
            CV_SHMEDIA_FR63 = 191,

            // Double - 64 bit synonyms for 32bit fp register pairs
            //          subtract 128 to find first base single register
            CV_SHMEDIA_DR0 = 256,
            CV_SHMEDIA_DR2 = 258,
            CV_SHMEDIA_DR4 = 260,
            CV_SHMEDIA_DR6 = 262,
            CV_SHMEDIA_DR8 = 264,
            CV_SHMEDIA_DR10 = 266,
            CV_SHMEDIA_DR12 = 268,
            CV_SHMEDIA_DR14 = 270,
            CV_SHMEDIA_DR16 = 272,
            CV_SHMEDIA_DR18 = 274,
            CV_SHMEDIA_DR20 = 276,
            CV_SHMEDIA_DR22 = 278,
            CV_SHMEDIA_DR24 = 280,
            CV_SHMEDIA_DR26 = 282,
            CV_SHMEDIA_DR28 = 284,
            CV_SHMEDIA_DR30 = 286,
            CV_SHMEDIA_DR32 = 288,
            CV_SHMEDIA_DR34 = 290,
            CV_SHMEDIA_DR36 = 292,
            CV_SHMEDIA_DR38 = 294,
            CV_SHMEDIA_DR40 = 296,
            CV_SHMEDIA_DR42 = 298,
            CV_SHMEDIA_DR44 = 300,
            CV_SHMEDIA_DR46 = 302,
            CV_SHMEDIA_DR48 = 304,
            CV_SHMEDIA_DR50 = 306,
            CV_SHMEDIA_DR52 = 308,
            CV_SHMEDIA_DR54 = 310,
            CV_SHMEDIA_DR56 = 312,
            CV_SHMEDIA_DR58 = 314,
            CV_SHMEDIA_DR60 = 316,
            CV_SHMEDIA_DR62 = 318,

            // Vector - 128 bit synonyms for 32bit fp register quads
            //          subtract 384 to find first base single register
            CV_SHMEDIA_FV0 = 512,
            CV_SHMEDIA_FV4 = 516,
            CV_SHMEDIA_FV8 = 520,
            CV_SHMEDIA_FV12 = 524,
            CV_SHMEDIA_FV16 = 528,
            CV_SHMEDIA_FV20 = 532,
            CV_SHMEDIA_FV24 = 536,
            CV_SHMEDIA_FV28 = 540,
            CV_SHMEDIA_FV32 = 544,
            CV_SHMEDIA_FV36 = 548,
            CV_SHMEDIA_FV40 = 552,
            CV_SHMEDIA_FV44 = 556,
            CV_SHMEDIA_FV48 = 560,
            CV_SHMEDIA_FV52 = 564,
            CV_SHMEDIA_FV56 = 568,
            CV_SHMEDIA_FV60 = 572,

            // Matrix - 512 bit synonyms for 16 adjacent 32bit fp registers
            //          subtract 896 to find first base single register
            CV_SHMEDIA_MTRX0 = 1024,
            CV_SHMEDIA_MTRX16 = 1040,
            CV_SHMEDIA_MTRX32 = 1056,
            CV_SHMEDIA_MTRX48 = 1072,

            // Control - Implementation defined 64bit control registers
            CV_SHMEDIA_CR0 = 2000,
            CV_SHMEDIA_CR1 = 2001,
            CV_SHMEDIA_CR2 = 2002,
            CV_SHMEDIA_CR3 = 2003,
            CV_SHMEDIA_CR4 = 2004,
            CV_SHMEDIA_CR5 = 2005,
            CV_SHMEDIA_CR6 = 2006,
            CV_SHMEDIA_CR7 = 2007,
            CV_SHMEDIA_CR8 = 2008,
            CV_SHMEDIA_CR9 = 2009,
            CV_SHMEDIA_CR10 = 2010,
            CV_SHMEDIA_CR11 = 2011,
            CV_SHMEDIA_CR12 = 2012,
            CV_SHMEDIA_CR13 = 2013,
            CV_SHMEDIA_CR14 = 2014,
            CV_SHMEDIA_CR15 = 2015,
            CV_SHMEDIA_CR16 = 2016,
            CV_SHMEDIA_CR17 = 2017,
            CV_SHMEDIA_CR18 = 2018,
            CV_SHMEDIA_CR19 = 2019,
            CV_SHMEDIA_CR20 = 2020,
            CV_SHMEDIA_CR21 = 2021,
            CV_SHMEDIA_CR22 = 2022,
            CV_SHMEDIA_CR23 = 2023,
            CV_SHMEDIA_CR24 = 2024,
            CV_SHMEDIA_CR25 = 2025,
            CV_SHMEDIA_CR26 = 2026,
            CV_SHMEDIA_CR27 = 2027,
            CV_SHMEDIA_CR28 = 2028,
            CV_SHMEDIA_CR29 = 2029,
            CV_SHMEDIA_CR30 = 2030,
            CV_SHMEDIA_CR31 = 2031,
            CV_SHMEDIA_CR32 = 2032,
            CV_SHMEDIA_CR33 = 2033,
            CV_SHMEDIA_CR34 = 2034,
            CV_SHMEDIA_CR35 = 2035,
            CV_SHMEDIA_CR36 = 2036,
            CV_SHMEDIA_CR37 = 2037,
            CV_SHMEDIA_CR38 = 2038,
            CV_SHMEDIA_CR39 = 2039,
            CV_SHMEDIA_CR40 = 2040,
            CV_SHMEDIA_CR41 = 2041,
            CV_SHMEDIA_CR42 = 2042,
            CV_SHMEDIA_CR43 = 2043,
            CV_SHMEDIA_CR44 = 2044,
            CV_SHMEDIA_CR45 = 2045,
            CV_SHMEDIA_CR46 = 2046,
            CV_SHMEDIA_CR47 = 2047,
            CV_SHMEDIA_CR48 = 2048,
            CV_SHMEDIA_CR49 = 2049,
            CV_SHMEDIA_CR50 = 2050,
            CV_SHMEDIA_CR51 = 2051,
            CV_SHMEDIA_CR52 = 2052,
            CV_SHMEDIA_CR53 = 2053,
            CV_SHMEDIA_CR54 = 2054,
            CV_SHMEDIA_CR55 = 2055,
            CV_SHMEDIA_CR56 = 2056,
            CV_SHMEDIA_CR57 = 2057,
            CV_SHMEDIA_CR58 = 2058,
            CV_SHMEDIA_CR59 = 2059,
            CV_SHMEDIA_CR60 = 2060,
            CV_SHMEDIA_CR61 = 2061,
            CV_SHMEDIA_CR62 = 2062,
            CV_SHMEDIA_CR63 = 2063,

            CV_SHMEDIA_FPSCR = 2064,

            // Compact mode synonyms
            CV_SHMEDIA_GBR = CV_SHMEDIA_R16,
            CV_SHMEDIA_MACL = 90, // synonym for lower 32bits of media R17
            CV_SHMEDIA_MACH = 91, // synonym for upper 32bits of media R17
            CV_SHMEDIA_PR = CV_SHMEDIA_R18,
            CV_SHMEDIA_T = 92, // synonym for lowest bit of media R19
            CV_SHMEDIA_FPUL = CV_SHMEDIA_FR32,
            CV_SHMEDIA_PC = 93,
            CV_SHMEDIA_SR = CV_SHMEDIA_CR0,

            //
            // AMD64 registers
            //

            CV_AMD64_AL = 1,
            CV_AMD64_CL = 2,
            CV_AMD64_DL = 3,
            CV_AMD64_BL = 4,
            CV_AMD64_AH = 5,
            CV_AMD64_CH = 6,
            CV_AMD64_DH = 7,
            CV_AMD64_BH = 8,
            CV_AMD64_AX = 9,
            CV_AMD64_CX = 10,
            CV_AMD64_DX = 11,
            CV_AMD64_BX = 12,
            CV_AMD64_SP = 13,
            CV_AMD64_BP = 14,
            CV_AMD64_SI = 15,
            CV_AMD64_DI = 16,
            CV_AMD64_EAX = 17,
            CV_AMD64_ECX = 18,
            CV_AMD64_EDX = 19,
            CV_AMD64_EBX = 20,
            CV_AMD64_ESP = 21,
            CV_AMD64_EBP = 22,
            CV_AMD64_ESI = 23,
            CV_AMD64_EDI = 24,
            CV_AMD64_ES = 25,
            CV_AMD64_CS = 26,
            CV_AMD64_SS = 27,
            CV_AMD64_DS = 28,
            CV_AMD64_FS = 29,
            CV_AMD64_GS = 30,
            CV_AMD64_FLAGS = 32,
            CV_AMD64_RIP = 33,
            CV_AMD64_EFLAGS = 34,

            // Control registers
            CV_AMD64_CR0 = 80,
            CV_AMD64_CR1 = 81,
            CV_AMD64_CR2 = 82,
            CV_AMD64_CR3 = 83,
            CV_AMD64_CR4 = 84,
            CV_AMD64_CR8 = 88,

            // Debug registers
            CV_AMD64_DR0 = 90,
            CV_AMD64_DR1 = 91,
            CV_AMD64_DR2 = 92,
            CV_AMD64_DR3 = 93,
            CV_AMD64_DR4 = 94,
            CV_AMD64_DR5 = 95,
            CV_AMD64_DR6 = 96,
            CV_AMD64_DR7 = 97,
            CV_AMD64_DR8 = 98,
            CV_AMD64_DR9 = 99,
            CV_AMD64_DR10 = 100,
            CV_AMD64_DR11 = 101,
            CV_AMD64_DR12 = 102,
            CV_AMD64_DR13 = 103,
            CV_AMD64_DR14 = 104,
            CV_AMD64_DR15 = 105,

            CV_AMD64_GDTR = 110,
            CV_AMD64_GDTL = 111,
            CV_AMD64_IDTR = 112,
            CV_AMD64_IDTL = 113,
            CV_AMD64_LDTR = 114,
            CV_AMD64_TR = 115,

            CV_AMD64_ST0 = 128,
            CV_AMD64_ST1 = 129,
            CV_AMD64_ST2 = 130,
            CV_AMD64_ST3 = 131,
            CV_AMD64_ST4 = 132,
            CV_AMD64_ST5 = 133,
            CV_AMD64_ST6 = 134,
            CV_AMD64_ST7 = 135,
            CV_AMD64_CTRL = 136,
            CV_AMD64_STAT = 137,
            CV_AMD64_TAG = 138,
            CV_AMD64_FPIP = 139,
            CV_AMD64_FPCS = 140,
            CV_AMD64_FPDO = 141,
            CV_AMD64_FPDS = 142,
            CV_AMD64_ISEM = 143,
            CV_AMD64_FPEIP = 144,
            CV_AMD64_FPEDO = 145,

            CV_AMD64_MM0 = 146,
            CV_AMD64_MM1 = 147,
            CV_AMD64_MM2 = 148,
            CV_AMD64_MM3 = 149,
            CV_AMD64_MM4 = 150,
            CV_AMD64_MM5 = 151,
            CV_AMD64_MM6 = 152,
            CV_AMD64_MM7 = 153,

            CV_AMD64_XMM0 = 154,   // KATMAI registers
            CV_AMD64_XMM1 = 155,
            CV_AMD64_XMM2 = 156,
            CV_AMD64_XMM3 = 157,
            CV_AMD64_XMM4 = 158,
            CV_AMD64_XMM5 = 159,
            CV_AMD64_XMM6 = 160,
            CV_AMD64_XMM7 = 161,

            CV_AMD64_XMM0_0 = 162,   // KATMAI sub-registers
            CV_AMD64_XMM0_1 = 163,
            CV_AMD64_XMM0_2 = 164,
            CV_AMD64_XMM0_3 = 165,
            CV_AMD64_XMM1_0 = 166,
            CV_AMD64_XMM1_1 = 167,
            CV_AMD64_XMM1_2 = 168,
            CV_AMD64_XMM1_3 = 169,
            CV_AMD64_XMM2_0 = 170,
            CV_AMD64_XMM2_1 = 171,
            CV_AMD64_XMM2_2 = 172,
            CV_AMD64_XMM2_3 = 173,
            CV_AMD64_XMM3_0 = 174,
            CV_AMD64_XMM3_1 = 175,
            CV_AMD64_XMM3_2 = 176,
            CV_AMD64_XMM3_3 = 177,
            CV_AMD64_XMM4_0 = 178,
            CV_AMD64_XMM4_1 = 179,
            CV_AMD64_XMM4_2 = 180,
            CV_AMD64_XMM4_3 = 181,
            CV_AMD64_XMM5_0 = 182,
            CV_AMD64_XMM5_1 = 183,
            CV_AMD64_XMM5_2 = 184,
            CV_AMD64_XMM5_3 = 185,
            CV_AMD64_XMM6_0 = 186,
            CV_AMD64_XMM6_1 = 187,
            CV_AMD64_XMM6_2 = 188,
            CV_AMD64_XMM6_3 = 189,
            CV_AMD64_XMM7_0 = 190,
            CV_AMD64_XMM7_1 = 191,
            CV_AMD64_XMM7_2 = 192,
            CV_AMD64_XMM7_3 = 193,

            CV_AMD64_XMM0L = 194,
            CV_AMD64_XMM1L = 195,
            CV_AMD64_XMM2L = 196,
            CV_AMD64_XMM3L = 197,
            CV_AMD64_XMM4L = 198,
            CV_AMD64_XMM5L = 199,
            CV_AMD64_XMM6L = 200,
            CV_AMD64_XMM7L = 201,

            CV_AMD64_XMM0H = 202,
            CV_AMD64_XMM1H = 203,
            CV_AMD64_XMM2H = 204,
            CV_AMD64_XMM3H = 205,
            CV_AMD64_XMM4H = 206,
            CV_AMD64_XMM5H = 207,
            CV_AMD64_XMM6H = 208,
            CV_AMD64_XMM7H = 209,

            CV_AMD64_MXCSR = 211,   // XMM status register

            CV_AMD64_EMM0L = 220,   // XMM sub-registers (WNI integer)
            CV_AMD64_EMM1L = 221,
            CV_AMD64_EMM2L = 222,
            CV_AMD64_EMM3L = 223,
            CV_AMD64_EMM4L = 224,
            CV_AMD64_EMM5L = 225,
            CV_AMD64_EMM6L = 226,
            CV_AMD64_EMM7L = 227,

            CV_AMD64_EMM0H = 228,
            CV_AMD64_EMM1H = 229,
            CV_AMD64_EMM2H = 230,
            CV_AMD64_EMM3H = 231,
            CV_AMD64_EMM4H = 232,
            CV_AMD64_EMM5H = 233,
            CV_AMD64_EMM6H = 234,
            CV_AMD64_EMM7H = 235,

            // do not change the order of these regs, first one must be even too
            CV_AMD64_MM00 = 236,
            CV_AMD64_MM01 = 237,
            CV_AMD64_MM10 = 238,
            CV_AMD64_MM11 = 239,
            CV_AMD64_MM20 = 240,
            CV_AMD64_MM21 = 241,
            CV_AMD64_MM30 = 242,
            CV_AMD64_MM31 = 243,
            CV_AMD64_MM40 = 244,
            CV_AMD64_MM41 = 245,
            CV_AMD64_MM50 = 246,
            CV_AMD64_MM51 = 247,
            CV_AMD64_MM60 = 248,
            CV_AMD64_MM61 = 249,
            CV_AMD64_MM70 = 250,
            CV_AMD64_MM71 = 251,

            // Extended KATMAI registers
            CV_AMD64_XMM8 = 252,   // KATMAI registers
            CV_AMD64_XMM9 = 253,
            CV_AMD64_XMM10 = 254,
            CV_AMD64_XMM11 = 255,
            CV_AMD64_XMM12 = 256,
            CV_AMD64_XMM13 = 257,
            CV_AMD64_XMM14 = 258,
            CV_AMD64_XMM15 = 259,

            CV_AMD64_XMM8_0 = 260,   // KATMAI sub-registers
            CV_AMD64_XMM8_1 = 261,
            CV_AMD64_XMM8_2 = 262,
            CV_AMD64_XMM8_3 = 263,
            CV_AMD64_XMM9_0 = 264,
            CV_AMD64_XMM9_1 = 265,
            CV_AMD64_XMM9_2 = 266,
            CV_AMD64_XMM9_3 = 267,
            CV_AMD64_XMM10_0 = 268,
            CV_AMD64_XMM10_1 = 269,
            CV_AMD64_XMM10_2 = 270,
            CV_AMD64_XMM10_3 = 271,
            CV_AMD64_XMM11_0 = 272,
            CV_AMD64_XMM11_1 = 273,
            CV_AMD64_XMM11_2 = 274,
            CV_AMD64_XMM11_3 = 275,
            CV_AMD64_XMM12_0 = 276,
            CV_AMD64_XMM12_1 = 277,
            CV_AMD64_XMM12_2 = 278,
            CV_AMD64_XMM12_3 = 279,
            CV_AMD64_XMM13_0 = 280,
            CV_AMD64_XMM13_1 = 281,
            CV_AMD64_XMM13_2 = 282,
            CV_AMD64_XMM13_3 = 283,
            CV_AMD64_XMM14_0 = 284,
            CV_AMD64_XMM14_1 = 285,
            CV_AMD64_XMM14_2 = 286,
            CV_AMD64_XMM14_3 = 287,
            CV_AMD64_XMM15_0 = 288,
            CV_AMD64_XMM15_1 = 289,
            CV_AMD64_XMM15_2 = 290,
            CV_AMD64_XMM15_3 = 291,

            CV_AMD64_XMM8L = 292,
            CV_AMD64_XMM9L = 293,
            CV_AMD64_XMM10L = 294,
            CV_AMD64_XMM11L = 295,
            CV_AMD64_XMM12L = 296,
            CV_AMD64_XMM13L = 297,
            CV_AMD64_XMM14L = 298,
            CV_AMD64_XMM15L = 299,

            CV_AMD64_XMM8H = 300,
            CV_AMD64_XMM9H = 301,
            CV_AMD64_XMM10H = 302,
            CV_AMD64_XMM11H = 303,
            CV_AMD64_XMM12H = 304,
            CV_AMD64_XMM13H = 305,
            CV_AMD64_XMM14H = 306,
            CV_AMD64_XMM15H = 307,

            CV_AMD64_EMM8L = 308,   // XMM sub-registers (WNI integer)
            CV_AMD64_EMM9L = 309,
            CV_AMD64_EMM10L = 310,
            CV_AMD64_EMM11L = 311,
            CV_AMD64_EMM12L = 312,
            CV_AMD64_EMM13L = 313,
            CV_AMD64_EMM14L = 314,
            CV_AMD64_EMM15L = 315,

            CV_AMD64_EMM8H = 316,
            CV_AMD64_EMM9H = 317,
            CV_AMD64_EMM10H = 318,
            CV_AMD64_EMM11H = 319,
            CV_AMD64_EMM12H = 320,
            CV_AMD64_EMM13H = 321,
            CV_AMD64_EMM14H = 322,
            CV_AMD64_EMM15H = 323,

            // Low byte forms of some standard registers
            CV_AMD64_SIL = 324,
            CV_AMD64_DIL = 325,
            CV_AMD64_BPL = 326,
            CV_AMD64_SPL = 327,

            // 64-bit regular registers
            CV_AMD64_RAX = 328,
            CV_AMD64_RBX = 329,
            CV_AMD64_RCX = 330,
            CV_AMD64_RDX = 331,
            CV_AMD64_RSI = 332,
            CV_AMD64_RDI = 333,
            CV_AMD64_RBP = 334,
            CV_AMD64_RSP = 335,

            // 64-bit integer registers with 8-, 16-, and 32-bit forms (B, W, and D)
            CV_AMD64_R8 = 336,
            CV_AMD64_R9 = 337,
            CV_AMD64_R10 = 338,
            CV_AMD64_R11 = 339,
            CV_AMD64_R12 = 340,
            CV_AMD64_R13 = 341,
            CV_AMD64_R14 = 342,
            CV_AMD64_R15 = 343,

            CV_AMD64_R8B = 344,
            CV_AMD64_R9B = 345,
            CV_AMD64_R10B = 346,
            CV_AMD64_R11B = 347,
            CV_AMD64_R12B = 348,
            CV_AMD64_R13B = 349,
            CV_AMD64_R14B = 350,
            CV_AMD64_R15B = 351,

            CV_AMD64_R8W = 352,
            CV_AMD64_R9W = 353,
            CV_AMD64_R10W = 354,
            CV_AMD64_R11W = 355,
            CV_AMD64_R12W = 356,
            CV_AMD64_R13W = 357,
            CV_AMD64_R14W = 358,
            CV_AMD64_R15W = 359,

            CV_AMD64_R8D = 360,
            CV_AMD64_R9D = 361,
            CV_AMD64_R10D = 362,
            CV_AMD64_R11D = 363,
            CV_AMD64_R12D = 364,
            CV_AMD64_R13D = 365,
            CV_AMD64_R14D = 366,
            CV_AMD64_R15D = 367,

            // AVX registers 256 bits
            CV_AMD64_YMM0 = 368,
            CV_AMD64_YMM1 = 369,
            CV_AMD64_YMM2 = 370,
            CV_AMD64_YMM3 = 371,
            CV_AMD64_YMM4 = 372,
            CV_AMD64_YMM5 = 373,
            CV_AMD64_YMM6 = 374,
            CV_AMD64_YMM7 = 375,
            CV_AMD64_YMM8 = 376,
            CV_AMD64_YMM9 = 377,
            CV_AMD64_YMM10 = 378,
            CV_AMD64_YMM11 = 379,
            CV_AMD64_YMM12 = 380,
            CV_AMD64_YMM13 = 381,
            CV_AMD64_YMM14 = 382,
            CV_AMD64_YMM15 = 383,

            // AVX registers upper 128 bits
            CV_AMD64_YMM0H = 384,
            CV_AMD64_YMM1H = 385,
            CV_AMD64_YMM2H = 386,
            CV_AMD64_YMM3H = 387,
            CV_AMD64_YMM4H = 388,
            CV_AMD64_YMM5H = 389,
            CV_AMD64_YMM6H = 390,
            CV_AMD64_YMM7H = 391,
            CV_AMD64_YMM8H = 392,
            CV_AMD64_YMM9H = 393,
            CV_AMD64_YMM10H = 394,
            CV_AMD64_YMM11H = 395,
            CV_AMD64_YMM12H = 396,
            CV_AMD64_YMM13H = 397,
            CV_AMD64_YMM14H = 398,
            CV_AMD64_YMM15H = 399,

            //Lower/upper 8 bytes of XMM registers.  Unlike CV_AMD64_XMM<regnum><H/L>, these
            //values reprsesent the bit patterns of the registers as 64-bit integers, not
            //the representation of these registers as a double.
            CV_AMD64_XMM0IL = 400,
            CV_AMD64_XMM1IL = 401,
            CV_AMD64_XMM2IL = 402,
            CV_AMD64_XMM3IL = 403,
            CV_AMD64_XMM4IL = 404,
            CV_AMD64_XMM5IL = 405,
            CV_AMD64_XMM6IL = 406,
            CV_AMD64_XMM7IL = 407,
            CV_AMD64_XMM8IL = 408,
            CV_AMD64_XMM9IL = 409,
            CV_AMD64_XMM10IL = 410,
            CV_AMD64_XMM11IL = 411,
            CV_AMD64_XMM12IL = 412,
            CV_AMD64_XMM13IL = 413,
            CV_AMD64_XMM14IL = 414,
            CV_AMD64_XMM15IL = 415,

            CV_AMD64_XMM0IH = 416,
            CV_AMD64_XMM1IH = 417,
            CV_AMD64_XMM2IH = 418,
            CV_AMD64_XMM3IH = 419,
            CV_AMD64_XMM4IH = 420,
            CV_AMD64_XMM5IH = 421,
            CV_AMD64_XMM6IH = 422,
            CV_AMD64_XMM7IH = 423,
            CV_AMD64_XMM8IH = 424,
            CV_AMD64_XMM9IH = 425,
            CV_AMD64_XMM10IH = 426,
            CV_AMD64_XMM11IH = 427,
            CV_AMD64_XMM12IH = 428,
            CV_AMD64_XMM13IH = 429,
            CV_AMD64_XMM14IH = 430,
            CV_AMD64_XMM15IH = 431,

            CV_AMD64_YMM0I0 = 432,        // AVX integer registers
            CV_AMD64_YMM0I1 = 433,
            CV_AMD64_YMM0I2 = 434,
            CV_AMD64_YMM0I3 = 435,
            CV_AMD64_YMM1I0 = 436,
            CV_AMD64_YMM1I1 = 437,
            CV_AMD64_YMM1I2 = 438,
            CV_AMD64_YMM1I3 = 439,
            CV_AMD64_YMM2I0 = 440,
            CV_AMD64_YMM2I1 = 441,
            CV_AMD64_YMM2I2 = 442,
            CV_AMD64_YMM2I3 = 443,
            CV_AMD64_YMM3I0 = 444,
            CV_AMD64_YMM3I1 = 445,
            CV_AMD64_YMM3I2 = 446,
            CV_AMD64_YMM3I3 = 447,
            CV_AMD64_YMM4I0 = 448,
            CV_AMD64_YMM4I1 = 449,
            CV_AMD64_YMM4I2 = 450,
            CV_AMD64_YMM4I3 = 451,
            CV_AMD64_YMM5I0 = 452,
            CV_AMD64_YMM5I1 = 453,
            CV_AMD64_YMM5I2 = 454,
            CV_AMD64_YMM5I3 = 455,
            CV_AMD64_YMM6I0 = 456,
            CV_AMD64_YMM6I1 = 457,
            CV_AMD64_YMM6I2 = 458,
            CV_AMD64_YMM6I3 = 459,
            CV_AMD64_YMM7I0 = 460,
            CV_AMD64_YMM7I1 = 461,
            CV_AMD64_YMM7I2 = 462,
            CV_AMD64_YMM7I3 = 463,
            CV_AMD64_YMM8I0 = 464,
            CV_AMD64_YMM8I1 = 465,
            CV_AMD64_YMM8I2 = 466,
            CV_AMD64_YMM8I3 = 467,
            CV_AMD64_YMM9I0 = 468,
            CV_AMD64_YMM9I1 = 469,
            CV_AMD64_YMM9I2 = 470,
            CV_AMD64_YMM9I3 = 471,
            CV_AMD64_YMM10I0 = 472,
            CV_AMD64_YMM10I1 = 473,
            CV_AMD64_YMM10I2 = 474,
            CV_AMD64_YMM10I3 = 475,
            CV_AMD64_YMM11I0 = 476,
            CV_AMD64_YMM11I1 = 477,
            CV_AMD64_YMM11I2 = 478,
            CV_AMD64_YMM11I3 = 479,
            CV_AMD64_YMM12I0 = 480,
            CV_AMD64_YMM12I1 = 481,
            CV_AMD64_YMM12I2 = 482,
            CV_AMD64_YMM12I3 = 483,
            CV_AMD64_YMM13I0 = 484,
            CV_AMD64_YMM13I1 = 485,
            CV_AMD64_YMM13I2 = 486,
            CV_AMD64_YMM13I3 = 487,
            CV_AMD64_YMM14I0 = 488,
            CV_AMD64_YMM14I1 = 489,
            CV_AMD64_YMM14I2 = 490,
            CV_AMD64_YMM14I3 = 491,
            CV_AMD64_YMM15I0 = 492,
            CV_AMD64_YMM15I1 = 493,
            CV_AMD64_YMM15I2 = 494,
            CV_AMD64_YMM15I3 = 495,

            CV_AMD64_YMM0F0 = 496,        // AVX floating-point single precise registers
            CV_AMD64_YMM0F1 = 497,
            CV_AMD64_YMM0F2 = 498,
            CV_AMD64_YMM0F3 = 499,
            CV_AMD64_YMM0F4 = 500,
            CV_AMD64_YMM0F5 = 501,
            CV_AMD64_YMM0F6 = 502,
            CV_AMD64_YMM0F7 = 503,
            CV_AMD64_YMM1F0 = 504,
            CV_AMD64_YMM1F1 = 505,
            CV_AMD64_YMM1F2 = 506,
            CV_AMD64_YMM1F3 = 507,
            CV_AMD64_YMM1F4 = 508,
            CV_AMD64_YMM1F5 = 509,
            CV_AMD64_YMM1F6 = 510,
            CV_AMD64_YMM1F7 = 511,
            CV_AMD64_YMM2F0 = 512,
            CV_AMD64_YMM2F1 = 513,
            CV_AMD64_YMM2F2 = 514,
            CV_AMD64_YMM2F3 = 515,
            CV_AMD64_YMM2F4 = 516,
            CV_AMD64_YMM2F5 = 517,
            CV_AMD64_YMM2F6 = 518,
            CV_AMD64_YMM2F7 = 519,
            CV_AMD64_YMM3F0 = 520,
            CV_AMD64_YMM3F1 = 521,
            CV_AMD64_YMM3F2 = 522,
            CV_AMD64_YMM3F3 = 523,
            CV_AMD64_YMM3F4 = 524,
            CV_AMD64_YMM3F5 = 525,
            CV_AMD64_YMM3F6 = 526,
            CV_AMD64_YMM3F7 = 527,
            CV_AMD64_YMM4F0 = 528,
            CV_AMD64_YMM4F1 = 529,
            CV_AMD64_YMM4F2 = 530,
            CV_AMD64_YMM4F3 = 531,
            CV_AMD64_YMM4F4 = 532,
            CV_AMD64_YMM4F5 = 533,
            CV_AMD64_YMM4F6 = 534,
            CV_AMD64_YMM4F7 = 535,
            CV_AMD64_YMM5F0 = 536,
            CV_AMD64_YMM5F1 = 537,
            CV_AMD64_YMM5F2 = 538,
            CV_AMD64_YMM5F3 = 539,
            CV_AMD64_YMM5F4 = 540,
            CV_AMD64_YMM5F5 = 541,
            CV_AMD64_YMM5F6 = 542,
            CV_AMD64_YMM5F7 = 543,
            CV_AMD64_YMM6F0 = 544,
            CV_AMD64_YMM6F1 = 545,
            CV_AMD64_YMM6F2 = 546,
            CV_AMD64_YMM6F3 = 547,
            CV_AMD64_YMM6F4 = 548,
            CV_AMD64_YMM6F5 = 549,
            CV_AMD64_YMM6F6 = 550,
            CV_AMD64_YMM6F7 = 551,
            CV_AMD64_YMM7F0 = 552,
            CV_AMD64_YMM7F1 = 553,
            CV_AMD64_YMM7F2 = 554,
            CV_AMD64_YMM7F3 = 555,
            CV_AMD64_YMM7F4 = 556,
            CV_AMD64_YMM7F5 = 557,
            CV_AMD64_YMM7F6 = 558,
            CV_AMD64_YMM7F7 = 559,
            CV_AMD64_YMM8F0 = 560,
            CV_AMD64_YMM8F1 = 561,
            CV_AMD64_YMM8F2 = 562,
            CV_AMD64_YMM8F3 = 563,
            CV_AMD64_YMM8F4 = 564,
            CV_AMD64_YMM8F5 = 565,
            CV_AMD64_YMM8F6 = 566,
            CV_AMD64_YMM8F7 = 567,
            CV_AMD64_YMM9F0 = 568,
            CV_AMD64_YMM9F1 = 569,
            CV_AMD64_YMM9F2 = 570,
            CV_AMD64_YMM9F3 = 571,
            CV_AMD64_YMM9F4 = 572,
            CV_AMD64_YMM9F5 = 573,
            CV_AMD64_YMM9F6 = 574,
            CV_AMD64_YMM9F7 = 575,
            CV_AMD64_YMM10F0 = 576,
            CV_AMD64_YMM10F1 = 577,
            CV_AMD64_YMM10F2 = 578,
            CV_AMD64_YMM10F3 = 579,
            CV_AMD64_YMM10F4 = 580,
            CV_AMD64_YMM10F5 = 581,
            CV_AMD64_YMM10F6 = 582,
            CV_AMD64_YMM10F7 = 583,
            CV_AMD64_YMM11F0 = 584,
            CV_AMD64_YMM11F1 = 585,
            CV_AMD64_YMM11F2 = 586,
            CV_AMD64_YMM11F3 = 587,
            CV_AMD64_YMM11F4 = 588,
            CV_AMD64_YMM11F5 = 589,
            CV_AMD64_YMM11F6 = 590,
            CV_AMD64_YMM11F7 = 591,
            CV_AMD64_YMM12F0 = 592,
            CV_AMD64_YMM12F1 = 593,
            CV_AMD64_YMM12F2 = 594,
            CV_AMD64_YMM12F3 = 595,
            CV_AMD64_YMM12F4 = 596,
            CV_AMD64_YMM12F5 = 597,
            CV_AMD64_YMM12F6 = 598,
            CV_AMD64_YMM12F7 = 599,
            CV_AMD64_YMM13F0 = 600,
            CV_AMD64_YMM13F1 = 601,
            CV_AMD64_YMM13F2 = 602,
            CV_AMD64_YMM13F3 = 603,
            CV_AMD64_YMM13F4 = 604,
            CV_AMD64_YMM13F5 = 605,
            CV_AMD64_YMM13F6 = 606,
            CV_AMD64_YMM13F7 = 607,
            CV_AMD64_YMM14F0 = 608,
            CV_AMD64_YMM14F1 = 609,
            CV_AMD64_YMM14F2 = 610,
            CV_AMD64_YMM14F3 = 611,
            CV_AMD64_YMM14F4 = 612,
            CV_AMD64_YMM14F5 = 613,
            CV_AMD64_YMM14F6 = 614,
            CV_AMD64_YMM14F7 = 615,
            CV_AMD64_YMM15F0 = 616,
            CV_AMD64_YMM15F1 = 617,
            CV_AMD64_YMM15F2 = 618,
            CV_AMD64_YMM15F3 = 619,
            CV_AMD64_YMM15F4 = 620,
            CV_AMD64_YMM15F5 = 621,
            CV_AMD64_YMM15F6 = 622,
            CV_AMD64_YMM15F7 = 623,

            CV_AMD64_YMM0D0 = 624,        // AVX floating-point double precise registers
            CV_AMD64_YMM0D1 = 625,
            CV_AMD64_YMM0D2 = 626,
            CV_AMD64_YMM0D3 = 627,
            CV_AMD64_YMM1D0 = 628,
            CV_AMD64_YMM1D1 = 629,
            CV_AMD64_YMM1D2 = 630,
            CV_AMD64_YMM1D3 = 631,
            CV_AMD64_YMM2D0 = 632,
            CV_AMD64_YMM2D1 = 633,
            CV_AMD64_YMM2D2 = 634,
            CV_AMD64_YMM2D3 = 635,
            CV_AMD64_YMM3D0 = 636,
            CV_AMD64_YMM3D1 = 637,
            CV_AMD64_YMM3D2 = 638,
            CV_AMD64_YMM3D3 = 639,
            CV_AMD64_YMM4D0 = 640,
            CV_AMD64_YMM4D1 = 641,
            CV_AMD64_YMM4D2 = 642,
            CV_AMD64_YMM4D3 = 643,
            CV_AMD64_YMM5D0 = 644,
            CV_AMD64_YMM5D1 = 645,
            CV_AMD64_YMM5D2 = 646,
            CV_AMD64_YMM5D3 = 647,
            CV_AMD64_YMM6D0 = 648,
            CV_AMD64_YMM6D1 = 649,
            CV_AMD64_YMM6D2 = 650,
            CV_AMD64_YMM6D3 = 651,
            CV_AMD64_YMM7D0 = 652,
            CV_AMD64_YMM7D1 = 653,
            CV_AMD64_YMM7D2 = 654,
            CV_AMD64_YMM7D3 = 655,
            CV_AMD64_YMM8D0 = 656,
            CV_AMD64_YMM8D1 = 657,
            CV_AMD64_YMM8D2 = 658,
            CV_AMD64_YMM8D3 = 659,
            CV_AMD64_YMM9D0 = 660,
            CV_AMD64_YMM9D1 = 661,
            CV_AMD64_YMM9D2 = 662,
            CV_AMD64_YMM9D3 = 663,
            CV_AMD64_YMM10D0 = 664,
            CV_AMD64_YMM10D1 = 665,
            CV_AMD64_YMM10D2 = 666,
            CV_AMD64_YMM10D3 = 667,
            CV_AMD64_YMM11D0 = 668,
            CV_AMD64_YMM11D1 = 669,
            CV_AMD64_YMM11D2 = 670,
            CV_AMD64_YMM11D3 = 671,
            CV_AMD64_YMM12D0 = 672,
            CV_AMD64_YMM12D1 = 673,
            CV_AMD64_YMM12D2 = 674,
            CV_AMD64_YMM12D3 = 675,
            CV_AMD64_YMM13D0 = 676,
            CV_AMD64_YMM13D1 = 677,
            CV_AMD64_YMM13D2 = 678,
            CV_AMD64_YMM13D3 = 679,
            CV_AMD64_YMM14D0 = 680,
            CV_AMD64_YMM14D1 = 681,
            CV_AMD64_YMM14D2 = 682,
            CV_AMD64_YMM14D3 = 683,
            CV_AMD64_YMM15D0 = 684,
            CV_AMD64_YMM15D1 = 685,
            CV_AMD64_YMM15D2 = 686,
            CV_AMD64_YMM15D3 = 687


            // Note:  Next set of platform registers need to go into a new enum...
            // this one is above 44K now.

        }
    }
}
