using Godot;
using System;
using System.Collections.Generic;

public partial class AudioManager : Node
{
	[Signal]
	public delegate void AudioSettingsChangedEventHandler();

	private const int MixRate = 44100;
	private const float SfxBufferLength = 0.12f;
	private const int MaxActiveTones = 16;

	private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

	private readonly Dictionary<string, SfxDefinition> sfxDefinitions = new();
	private readonly Dictionary<string, double> sfxCooldowns = new();
	private readonly List<ActiveTone> activeTones = new();
	private readonly RandomNumberGenerator random = new();

	private AudioStreamPlayer sfxPlayer;
	private AudioStreamGeneratorPlayback sfxPlayback;
	private int masterVolumeIndex = 3;
	private int sfxVolumeIndex = 3;
	private bool muted;
	private double timeSeconds;

	public float MasterVolume => VolumeSteps[masterVolumeIndex];
	public float SfxVolume => VolumeSteps[sfxVolumeIndex];
	public bool IsMuted => muted;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		random.Randomize();
		RegisterSfxDefinitions();
		CreateSfxPlayer();
	}

	public override void _Process(double delta)
	{
		timeSeconds += delta;
		EnsureSfxPlayback();
		FillSfxBuffer();
	}

	public void PlaySfx(string name)
	{
		if (muted || SfxVolume <= 0f || MasterVolume <= 0f || string.IsNullOrWhiteSpace(name))
		{
			return;
		}

		if (!sfxDefinitions.TryGetValue(name, out SfxDefinition definition))
		{
			return;
		}

		if (sfxCooldowns.TryGetValue(name, out double nextAllowedTime) && timeSeconds < nextAllowedTime)
		{
			return;
		}

		sfxCooldowns[name] = timeSeconds + definition.Cooldown;
		if (activeTones.Count >= MaxActiveTones)
		{
			activeTones.RemoveAt(0);
		}

		activeTones.Add(new ActiveTone(definition));
	}

	public void SetMasterVolume(float value)
	{
		masterVolumeIndex = GetClosestVolumeIndex(value);
		EmitSignal(SignalName.AudioSettingsChanged);
	}

	public void SetSfxVolume(float value)
	{
		sfxVolumeIndex = GetClosestVolumeIndex(value);
		EmitSignal(SignalName.AudioSettingsChanged);
	}

	public void SetMuted(bool isMuted)
	{
		if (muted == isMuted)
		{
			return;
		}

		muted = isMuted;
		EmitSignal(SignalName.AudioSettingsChanged);
	}

	public void ToggleMute()
	{
		SetMuted(!muted);
	}

	public void CycleMasterVolume()
	{
		masterVolumeIndex = (masterVolumeIndex + 1) % VolumeSteps.Length;
		EmitSignal(SignalName.AudioSettingsChanged);
	}

	public void CycleSfxVolume()
	{
		sfxVolumeIndex = (sfxVolumeIndex + 1) % VolumeSteps.Length;
		EmitSignal(SignalName.AudioSettingsChanged);
	}

	public string GetMasterVolumeLabel()
	{
		return $"Master Volume: {Mathf.RoundToInt(MasterVolume * 100f)}%";
	}

	public string GetSfxVolumeLabel()
	{
		return $"SFX Volume: {Mathf.RoundToInt(SfxVolume * 100f)}%";
	}

	public string GetMuteLabel()
	{
		return $"Mute Audio: {(muted ? "On" : "Off")}";
	}

	private void CreateSfxPlayer()
	{
		sfxPlayer = new AudioStreamPlayer
		{
			Name = "GeneratedSfxPlayer",
			ProcessMode = ProcessModeEnum.Always,
			Bus = "Master",
			Stream = new AudioStreamGenerator
			{
				MixRate = MixRate,
				BufferLength = SfxBufferLength
			}
		};
		AddChild(sfxPlayer);
		sfxPlayer.Play();
		sfxPlayback = sfxPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
	}

	private void EnsureSfxPlayback()
	{
		if (sfxPlayer != null)
		{
			if (!sfxPlayer.Playing)
			{
				sfxPlayer.Play();
			}

			sfxPlayback ??= sfxPlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
		}
	}

	private void FillSfxBuffer()
	{
		if (sfxPlayback == null)
		{
			return;
		}

		int framesAvailable = sfxPlayback.GetFramesAvailable();
		float effectiveVolume = muted ? 0f : MasterVolume * SfxVolume;
		float frameTime = 1f / MixRate;
		for (int frame = 0; frame < framesAvailable; frame++)
		{
			float sample = 0f;
			for (int index = activeTones.Count - 1; index >= 0; index--)
			{
				ActiveTone tone = activeTones[index];
				tone.Elapsed += frameTime;
				if (tone.Elapsed >= tone.Definition.Duration)
				{
					activeTones.RemoveAt(index);
					continue;
				}

				float progress = tone.Elapsed / tone.Definition.Duration;
				float envelope = Mathf.Clamp(1f - progress, 0f, 1f);
				float frequency = tone.Definition.Frequency + tone.Definition.Sweep * progress;
				tone.Phase += Mathf.Tau * frequency * frameTime;
				float waveform = tone.Definition.Noise
					? random.RandfRange(-1f, 1f)
					: Mathf.Sin(tone.Phase);
				sample += waveform * tone.Definition.Volume * envelope;
			}

			sample = Mathf.Clamp(sample * effectiveVolume, -0.75f, 0.75f);
			sfxPlayback.PushFrame(new Vector2(sample, sample));
		}
	}

	private void RegisterSfxDefinitions()
	{
		sfxDefinitions["gather"] = new SfxDefinition(740f, 0.06f, 0.18f, 0.04f, 80f);
		sfxDefinitions["repair"] = new SfxDefinition(520f, 0.07f, 0.18f, 0.08f, 160f);
		sfxDefinitions["error"] = new SfxDefinition(145f, 0.09f, 0.22f, 0.25f, -40f);
		sfxDefinitions["building_placed"] = new SfxDefinition(300f, 0.11f, 0.22f, 0.05f, 240f);
		sfxDefinitions["building_sold"] = new SfxDefinition(390f, 0.09f, 0.2f, 0.08f, -80f);
		sfxDefinitions["turret_fire"] = new SfxDefinition(880f, 0.035f, 0.16f, 0.08f, -280f);
		sfxDefinitions["enemy_hit"] = new SfxDefinition(260f, 0.045f, 0.11f, 0.07f, -90f, true);
		sfxDefinitions["enemy_death"] = new SfxDefinition(170f, 0.14f, 0.18f, 0.05f, -90f, true);
		sfxDefinitions["player_damage"] = new SfxDefinition(120f, 0.13f, 0.25f, 0.08f, -40f, true);
		sfxDefinitions["core_damage"] = new SfxDefinition(95f, 0.18f, 0.28f, 0.12f, -30f, true);
		sfxDefinitions["building_destroyed"] = new SfxDefinition(135f, 0.2f, 0.28f, 0.08f, -55f, true);
		sfxDefinitions["day_start"] = new SfxDefinition(460f, 0.18f, 0.22f, 0.2f, 180f);
		sfxDefinitions["night_start"] = new SfxDefinition(210f, 0.24f, 0.26f, 0.2f, -80f);
		sfxDefinitions["wave_cleared"] = new SfxDefinition(520f, 0.2f, 0.2f, 0.2f, 220f);
		sfxDefinitions["victory"] = new SfxDefinition(680f, 0.35f, 0.22f, 0.2f, 260f);
		sfxDefinitions["defeat"] = new SfxDefinition(110f, 0.42f, 0.28f, 0.2f, -55f);
		sfxDefinitions["upgrade_selected"] = new SfxDefinition(760f, 0.16f, 0.22f, 0.08f, 180f);
	}

	private static int GetClosestVolumeIndex(float value)
	{
		float clamped = Mathf.Clamp(value, 0f, 1f);
		int closestIndex = 0;
		float closestDistance = float.MaxValue;
		for (int index = 0; index < VolumeSteps.Length; index++)
		{
			float distance = Mathf.Abs(VolumeSteps[index] - clamped);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestIndex = index;
			}
		}

		return closestIndex;
	}

	private sealed class SfxDefinition
	{
		public SfxDefinition(float frequency, float duration, float volume, float cooldown, float sweep = 0f, bool noise = false)
		{
			Frequency = frequency;
			Duration = duration;
			Volume = volume;
			Cooldown = cooldown;
			Sweep = sweep;
			Noise = noise;
		}

		public float Frequency { get; }
		public float Duration { get; }
		public float Volume { get; }
		public float Cooldown { get; }
		public float Sweep { get; }
		public bool Noise { get; }
	}

	private sealed class ActiveTone
	{
		public ActiveTone(SfxDefinition definition)
		{
			Definition = definition;
		}

		public SfxDefinition Definition { get; }
		public float Elapsed { get; set; }
		public float Phase { get; set; }
	}
}
