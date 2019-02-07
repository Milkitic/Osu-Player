using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Music.WaveProviders
{
    public class CusBufferedWaveProvider : IWaveProvider
    {
        private WaveFormat _waveFormat;
        private Queue<AudioBufferCus> _audioBufferQueue;

        internal Queue<AudioBufferCus> BufferQueue { get { return _audioBufferQueue; } }

        //public event EventHandler PlayPositionChanged;

        public CusBufferedWaveProvider(WaveFormat format)
        {
            _waveFormat = format;
            _audioBufferQueue = new Queue<AudioBufferCus>();
            MaxQueuedBuffers = 100;
        }

        public int MaxQueuedBuffers { get; set; }

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }


        public void AddSamples(byte[] buffer, int offset, int count, TimeSpan currentTime)
        {
            byte[] nbuffer = new byte[count];
            Buffer.BlockCopy(buffer, offset, nbuffer, 0, count);
            lock (_audioBufferQueue)
            {
                //if (_audioBufferQueue.Count >= MaxQueuedBuffers)
                //{
                //    throw new InvalidOperationException("Too many queued buffers");
                //}
                _audioBufferQueue.Enqueue(new AudioBufferCus(nbuffer, currentTime));
            }
        }


        public int BuffersCount { get { return _audioBufferQueue.Count; } }


        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int required = count - read;
                AudioBufferCus audioBuffer = null;
                lock (_audioBufferQueue)
                {
                    if (_audioBufferQueue.Count > 0)
                    {
                        audioBuffer = _audioBufferQueue.Peek();
                    }
                }

                if (audioBuffer == null)
                {
                    // Return a zero filled buffer
                    for (int n = 0; n < required; n++)
                        buffer[offset + n] = 0;
                    read += required;
                }
                else // There is an audio buffer - let's play it
                {
                    int nread = audioBuffer.Buffer.Length - audioBuffer.Position;

                    //// Fire PlayPositionChanged event
                    //if (PlayPositionChanged != null)
                    //{
                    //    PlayPositionChanged(this, new BufferedPlayEventArgs(audioBuffer.CurrentTime));
                    //}

                    // If this buffer must be read in it's entirety
                    if (nread <= required)
                    {
                        // Read entire buffer
                        Buffer.BlockCopy(audioBuffer.Buffer, audioBuffer.Position, buffer, offset + read, nread);
                        read += nread;

                        lock (_audioBufferQueue)
                        {
                            _audioBufferQueue.Dequeue();
                        }
                    }
                    else // the number of bytes that can be read is greater than that required
                    {
                        Buffer.BlockCopy(audioBuffer.Buffer, audioBuffer.Position, buffer, offset + read, required);
                        audioBuffer.Position += required;
                        read += required;
                    }
                }
            }
            return read;
        }

    }

    internal class AudioBufferCus
    {
        public byte[] Buffer { get; private set; }

        public int Position { get; set; }

        public TimeSpan CurrentTime { get; set; }

        public AudioBufferCus(byte[] newBuffer, TimeSpan currentTime)
        {
            Buffer = newBuffer;
            CurrentTime = currentTime;
            Position = 0;
        }
    }
}
