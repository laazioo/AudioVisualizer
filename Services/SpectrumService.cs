using NAudio.Dsp;
using System;
using System.Linq;

namespace AudioVisualizer.Services
{
    public class SpectrumService
    {
        private const int FftLength = 4096; // Increased from 1024 for better bass resolution (~10Hz per bin)
        private Complex[] _fftBuffer;

        public SpectrumService()
        {
            _fftBuffer = new Complex[FftLength];
        }

        public double[] CalculateSpectrum(byte[] buffer, int bytesRecorded)
        {
            // Assuming 32-bit float audio (standard for WASAPI Loopback)
            int bytesPerSample = 4;
            int channels = 2; // WASAPI Loopback is usually stereo
            int bytesPerFrame = bytesPerSample * channels;
            int sampleCount = bytesRecorded / bytesPerFrame;
            
            // Populate FFT buffer
            for (int i = 0; i < FftLength; i++)
            {
                if (i < sampleCount)
                {
                    // Read Left and Right channels and average them
                    int bufferOffset = i * bytesPerFrame;
                    float left = BitConverter.ToSingle(buffer, bufferOffset);
                    float right = BitConverter.ToSingle(buffer, bufferOffset + bytesPerSample);
                    float sample = (left + right) / 2.0f;

                    // Apply window function (Hanning)
                    double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (FftLength - 1)));
                    _fftBuffer[i].X = (float)(sample * window);
                    _fftBuffer[i].Y = 0;
                }
                else
                {
                    _fftBuffer[i].X = 0;
                    _fftBuffer[i].Y = 0;
                }
            }

            // Perform FFT
            FastFourierTransform.FFT(true, (int)Math.Log(FftLength, 2.0), _fftBuffer);

            // Calculate magnitudes (only first half is useful)
            int outputLength = FftLength / 2;
            double[] spectrum = new double[outputLength];
            
            for (int i = 0; i < outputLength; i++)
            {
                // Magnitude = sqrt(Re^2 + Im^2)
                double magnitude = Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);
                // Convert to Decibels or keep linear? For visuals, log scale often looks better, but linear is simpler to start.
                // Let's use a simple scaling for now.
                spectrum[i] = magnitude;
            }

            return spectrum;
        }
    }
}
