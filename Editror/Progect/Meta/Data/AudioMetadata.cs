using EngineLib;

namespace Editor
{

    public class AudioMetadata : AssetMetadata
    {
        public AudioMetadata()
        {
            AssetType = MetadataType.Audio;
        }

        // Общие настройки
        public bool ForceToMono { get; set; } = false;
        public bool Normalize { get; set; } = false;
        public bool LoadInMemory { get; set; } = false;
        public bool Preload { get; set; } = true;
        public bool AmbientSound { get; set; } = false;

        // Качество и сжатие
        public bool Compressed { get; set; } = true;
        public string CompressionFormat { get; set; } = "Vorbis"; // Vorbis, MP3, ADPCM, PCM
        public int Quality { get; set; } = 70;
        public int SampleRate { get; set; } = 44100;

        // 3D-звук
        public bool Enable3D { get; set; } = false;
        public float DopplerFactor { get; set; } = 1.0f;
        public float Rolloff
        {
            get; set;
        }
    }
}
