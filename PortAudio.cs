﻿using System;
using System.Collections;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using PaError = System.Int32;
using PaDeviceIndex = System.Int32;
using PaHostApiIndex = System.Int32;
using PaTime = System.Double;
using PaSampleFormat = System.UInt32;
using PaStreamFlags = System.UInt32;
using PaStreamCallbackFlags = System.UInt32;

namespace CWExpert
{
    public class PA19
    {
        #region Constants

        public const PaDeviceIndex paNoDevice = (PaDeviceIndex)(-1);
        public const PaDeviceIndex paUseHostApiSpecificDeviceSpecification = (PaDeviceIndex)(-2);
        public const PaSampleFormat paFloat32 = (PaSampleFormat)0x01;
        public const PaSampleFormat paInt32 = (PaSampleFormat)0x02;
        public const PaSampleFormat paInt24 = (PaSampleFormat)0x04;
        public const PaSampleFormat paInt16 = (PaSampleFormat)0x08;
        public const PaSampleFormat paInt8 = (PaSampleFormat)0x10;
        public const PaSampleFormat paUInt8 = (PaSampleFormat)0x20;
        public const PaSampleFormat paCustomFormat = (PaSampleFormat)0x10000;
        public const PaSampleFormat paNonInterleaved = (PaSampleFormat)0x80000000;
        public const uint paFormatIsSupported = 0;
        public const uint paFramesPerBufferUnspecified = 0;
        public const PaStreamFlags paNoFlag = (PaStreamFlags)0;
        public const PaStreamFlags paClipOff = (PaStreamFlags)0x01;
        public const PaStreamFlags paDitherOff = (PaStreamFlags)0x02;
        public const PaStreamFlags paNeverDropInput = (PaStreamFlags)0x04;
        public const PaStreamFlags paPrimeOutputBuffersUsingStreamCallback = (PaStreamFlags)0x08;
        public const PaStreamFlags paPlatformSpecificFlags = (PaStreamFlags)0xFFFF0000;
        public const PaStreamCallbackFlags paInputUnderflow = (PaStreamCallbackFlags)0x01;
        public const PaStreamCallbackFlags paInputOverflow = (PaStreamCallbackFlags)0x02;
        public const PaStreamCallbackFlags paOutputUnderflow = (PaStreamCallbackFlags)0x04;
        public const PaStreamCallbackFlags paOutputOverflow = (PaStreamCallbackFlags)0x08;
        public const PaStreamCallbackFlags paPrimingOutput = (PaStreamCallbackFlags)0x10;

        #endregion

        #region Enums

        public enum PaErrorCode
        {
            paNoError = 0, paNotInitialized = -10000, paUnanticipatedHostError, paInvalidChannelCount,
            paInvalidSampleRate, paInvalidDevice, paInvalidFlag, paSampleFormatNotSupported,
            paBadIODeviceCombination, paInsufficientMemory, paBufferTooBig, paBufferTooSmall,
            paNullCallback, paBadStreamPtr, paTimedOut, paInternalError,
            paDeviceUnavailable, paIncompatibleHostApiSpecificStreamInfo, paStreamIsStopped, paStreamIsNotStopped,
            paInputOverflowed, paOutputUnderflowed, paHostApiNotFound, paInvalidHostApi,
            paCanNotReadFromACallbackStream, paCanNotWriteToACallbackStream, paCanNotReadFromAnOutputOnlyStream, paCanNotWriteToAnInputOnlyStream,
            paIncompatibleStreamHostApi
        }

        public enum PaHostApiTypeId
        {
            paInDevelopment = 0, paDirectSound = 1, paMME = 2, paASIO = 3,
            paSoundManager = 4, paCoreAudio = 5, paOSS = 7, paALSA = 8,
            paAL = 9, paBeOS = 10
        }

        public enum PaStreamCallbackResult
        { paContinue = 0, paComplete = 1, paAbort = 2 }

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct PaHostApiInfo
        {
            public int structVersion;
            public int type;
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            public int deviceCount;
            public PaDeviceIndex defaultInputDevice;
            public PaDeviceIndex defaultOutputDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaHostErrorInfo
        {
            public PaHostApiTypeId hostApiType;
            public int errorCode;
            [MarshalAs(UnmanagedType.LPStr)]
            public string errorText;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaDeviceInfo
        {
            public int structVersion;
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            public PaHostApiIndex hostApi;
            public int maxInputChannels;
            public int maxOutputChannels;
            public PaTime defaultLowInputLatency;
            public PaTime defaultLowOutputLatency;
            public PaTime defaultHighInputLatency;
            public PaTime defaultHighOutputLatency;
            public double defaultSampleRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe public struct PaStreamParameters
        {
            public PaDeviceIndex device;
            public int channelCount;
            public PaSampleFormat sampleFormat;
            public PaTime suggestedLatency;
            public void* hostApiSpecificStreamInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamCallbackTimeInfo
        {
            public PaTime inputBufferAdcTime;
            public PaTime currentTime;
            public PaTime outputBufferDacTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamInfo
        {
            public int structVersion;
            public PaTime inputLatency;
            public PaTime outputLatency;
            public double sampleRate;
        }

        #endregion

        #region Function Definitions

        [DllImport("PA19.dll")]
        public static extern int PA_GetVersion();

        [DllImport("PA19.dll")]
        public static extern String PA_GetVersionText();

        [DllImport("PA19.dll")]
        public static extern String PA_GetErrorText(PaError errorCode);

        [DllImport("PA19.dll")]
        public static extern PaError PA_Initialize();

        [DllImport("PA19.dll")]
        public static extern PaError PA_Terminate();

        [DllImport("PA19.dll")]
        public static extern PaHostApiIndex PA_GetHostApiCount();

        [DllImport("PA19.dll")]
        public static extern PaHostApiIndex PA_GetDefaultHostApi();

        [DllImport("PA19.dll", EntryPoint = "PA_GetHostApiInfo")]
        public static extern IntPtr PA_GetHostApiInfoPtr(int hostId);
        public static PaHostApiInfo PA_GetHostApiInfo(int hostId)
        {
            IntPtr ptr = PA_GetHostApiInfoPtr(hostId);
            PaHostApiInfo info = (PaHostApiInfo)Marshal.PtrToStructure(ptr, typeof(PaHostApiInfo));
            return info;
        }

        [DllImport("PA19.dll")]
        public static extern PaHostApiIndex PA_HostApiTypeIdToHostApiIndex(PaHostApiTypeId type);

        [DllImport("PA19.dll")]
        public static extern PaDeviceIndex PA_HostApiDeviceIndexToDeviceIndex(int hostAPI, int hostApiDeviceIndex);

        [DllImport("PA19.dll", EntryPoint = "PA_GetLastHostErrorInfo")]
        public static extern IntPtr PA_GetLastHostErrorInfoPtr();
        public static PaHostErrorInfo PA_GetLastHostErrorInfo()
        {
            IntPtr ptr = PA_GetLastHostErrorInfoPtr();
            PaHostErrorInfo info = (PaHostErrorInfo)Marshal.PtrToStructure(ptr, typeof(PaHostErrorInfo));
            return info;
        }

        [DllImport("PA19.dll")]
        public static extern PaDeviceIndex PA_GetDeviceCount();

        [DllImport("PA19.dll")]
        public static extern PaDeviceIndex PA_GetDefaultInputDevice();

        [DllImport("PA19.dll")]
        public static extern PaDeviceIndex PA_GetDefaultOutputDevice();

        [DllImport("PA19.dll", EntryPoint = "PA_GetDeviceInfo")]
        public static extern IntPtr PA_GetDeviceInfoPtr(int device);
        public static PaDeviceInfo PA_GetDeviceInfo(int device)
        {
            IntPtr ptr = PA_GetDeviceInfoPtr(device);
            PaDeviceInfo info = (PaDeviceInfo)Marshal.PtrToStructure(ptr, typeof(PaDeviceInfo));
            return info;
        }

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_IsFormatSupported(
            PaStreamParameters* inputParameters,
            PaStreamParameters* outputParameters,
            double sampleRate);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_OpenStream(
            out void* stream,
            PaStreamParameters* inputParameters,
            PaStreamParameters* outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            PaStreamFlags streamFlags,
            PaStreamCallback streamCallback,
            int callback_id);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_OpenDefaultStream(
            out void* stream,
            int numInputChannels,
            int numOutputChannels,
            PaSampleFormat sampleFormat,
            double sampleRate,
            uint framesPerBuffer,
            PaStreamCallback streamCallback,
            int callback_id);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_CloseStream(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_SetStreamFinishedCallback(
            void* stream, PaStreamFinishedCallback streamFinishedCallback);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_StartStream(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_StopStream(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_AbortStream(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_IsStreamStopped(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_IsStreamActive(void* stream);

        [DllImport("PA19.dll", EntryPoint = "PA_GetStreamInfo")]
        unsafe public static extern IntPtr PA_GetStreamInfoPtr(void* stream);
        unsafe public static PaStreamInfo PA_GetStreamInfo(void* stream)
        {
            IntPtr ptr = PA_GetStreamInfoPtr(stream);
            PaStreamInfo info = (PaStreamInfo)Marshal.PtrToStructure(ptr, typeof(PaStreamInfo));
            return info;
        }

        [DllImport("PA19.dll")]
        unsafe public static extern PaTime PA_GetStreamTime(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern double PA_GetStreamCpuLoad(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_ReadStream(void* stream, void* buffer, uint frames);

        [DllImport("PA19.dll")]
        unsafe public static extern PaError PA_WriteStream(void* stream, void* buffer, uint frames);

        [DllImport("PA19.dll")]
        unsafe public static extern int PA_GetStreamReadAvailable(void* stream);

        [DllImport("PA19.dll")]
        unsafe public static extern int PA_GetStreamWriteAvailable(void* stream);

        [DllImport("PA19.dll")]
        public static extern PaError PA_GetSampleSize(PaSampleFormat format);

        [DllImport("PA19.dll")]
        public static extern void PA_Sleep(int msec);

        unsafe public delegate int PaStreamCallback(void* input, void* output, int frameCount,
            PaStreamCallbackTimeInfo* timeInfo, int statusFlags, void* userData);

        unsafe public delegate void PaStreamFinishedCallback(void* userData);

        #endregion
    }
}
