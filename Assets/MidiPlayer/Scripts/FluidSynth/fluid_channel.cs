using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace MidiPlayerTK
{

    // specific channel properties - internal class
    public class mptk_channel // V2.82 new
    {
        public bool enabled; // V2.82 move from fluid_channel 
        public float volume; // volume for the channel, between 0 and 1

        public int forcedPreset; // forced preset for this channel
        public int forcedBank; // forced bank for this channel
        public int lastPreset; // last preset for this channel
        public int lastBank; // last bank for this channel

        public int count; // count of note-on for the channel
        //private int channum;
        //private MidiSynth synth;
        public mptk_channel(MidiSynth psynth, int pchanum)
        {
            //synth = psynth;
            //channum = pchanum;
            enabled = true;
            volume = 1f;
            count = 0;
            forcedPreset = -1;
            forcedBank = -1;
        }
    }

    public class fluid_channel
    {
        public int channum;
        public int banknum;
        public int prognum;
        public HiPreset preset;
        private MidiSynth synth;
        public short key_pressure;
        public short channel_pressure;
        public short pitch_bend;
        public short pitch_wheel_sensitivity;

        // NRPN system 
        //int nrpn_select;
        // cached values of last MSB values of MSB/LSB controllers
        //byte bank_msb;

        // controller values
        public short[] cc;

        // the micro-tuning
        public fluid_tuning tuning;

        /* The values of the generators, set by NRPN messages, or by
         * fluid_synth_set_gen(), are cached in the channel so they can be
         * applied to future notes. They are copied to a voice's generators
         * in fluid_voice_init(), wihich calls fluid_gen_init().  */
        public double[] gens;

        /* By default, the NRPN values are relative to the values of the
         * generators set in the SoundFont. For example, if the NRPN
         * specifies an attack of 100 msec then 100 msec will be added to the
         * combined attack time of the sound font and the modulators.
         *
         * However, it is useful to be able to specify the generator value
         * absolutely, completely ignoring the generators of the sound font
         * and the values of modulators. The gen_abs field, is a boolean
         * flag indicating whether the NRPN value is absolute or not.
         */
        public bool[] gen_abs;

        public mptk_channel mptkChannel;

        public fluid_channel()
        {
        }

        public fluid_channel(MidiSynth psynth, int pchanum)
        {
            gens = new double[Enum.GetNames(typeof(fluid_gen_type)).Length];
            gen_abs = new bool[Enum.GetNames(typeof(fluid_gen_type)).Length];
            cc = new short[128];

            synth = psynth;
            channum = pchanum;
            preset = null;
            tuning = null;

            fluid_channel_init();
            fluid_channel_init_ctrl();
        }

        void fluid_channel_init()
        {
            prognum = 0;
            if (MidiPlayerGlobal.ImSFCurrent != null)
            {
                banknum = channum == 9 ? MidiPlayerGlobal.ImSFCurrent.DrumKitBankNumber : MidiPlayerGlobal.ImSFCurrent.DefaultBankNumber;
                preset = synth.fluid_synth_find_preset(banknum, prognum);
            }
        }

        void fluid_channel_init_ctrl()
        {
            /*
                @param is_all_ctrl_off if nonzero, only resets some controllers, according to
                https://www.midi.org/techspecs/rp15.php
                For MPTK: is_all_ctrl_off=0, all controllers will be reset
            */

            key_pressure = 0;
            channel_pressure = 0;
            pitch_bend = 0x2000; // Range is 0x4000, pitch bend wheel starts in centered position
            pitch_wheel_sensitivity = 2; // two semi-tones 

            for (int i = 0; i < gens.Length; i++)
            {
                gens[i] = 0.0f;
                gen_abs[i] = false;
            }

            for (int i = 0; i < 128; i++)
            {
                cc[i] = 0;
            }

            //fluid_channel_clear_portamento(chan); /* Clear PTC receive */
            //chan->previous_cc_breath = 0;         /* Reset previous breath */
            /* Reset polyphonic key pressure on all voices */
            //for (i = 0; i < 128; i++)
            //{
            //    fluid_channel_set_key_pressure(chan, i, 0);
            //}

            /* Set RPN controllers to NULL state */
            cc[(int)MPTKController.RPN_LSB] = 127;
            cc[(int)MPTKController.RPN_MSB] = 127;

            /* Set NRPN controllers to NULL state */
            cc[(int)MPTKController.NRPN_LSB] = 127;
            cc[(int)MPTKController.NRPN_MSB] = 127;

            /* Expression (MSB & LSB) */
            cc[(int)MPTKController.Expression] = 127;
            cc[(int)MPTKController.EXPRESSION_LSB] = 127;

            /* Just like panning, a value of 64 indicates no change for sound ctrls */
            for (int i = (int)MPTKController.SOUND_CTRL1; i <= (int)MPTKController.SOUND_CTRL10; i++)
            {
                cc[i] = 64;
            }

            // Volume / initial attenuation (MSB & LSB) 
            cc[(int)MPTKController.VOLUME_MSB] = 100; // V2.88.2 before was 127
            cc[(int)MPTKController.VOLUME_LSB] = 0;

            // Pan (MSB & LSB) 
            cc[(int)MPTKController.Pan] = 64;
            cc[(int)MPTKController.PAN_LSB] = 0;

            cc[(int)MPTKController.BALANCE_MSB] = 64;
            cc[(int)MPTKController.BALANCE_LSB] = 0;

            /* Reverb */
            /* fluid_channel_set_cc (chan, EFFECTS_DEPTH1, 40); */
            /* Note: although XG standard specifies the default amount of reverb to
               be 40, most people preferred having it at zero.
               See https://lists.gnu.org/archive/html/fluid-dev/2009-07/msg00016.html */
        }

        /*
         * fluid_channel_cc
         */
        public void fluid_channel_cc(MPTKController numController, int valueController)
        {
            cc[(int)numController] = (short)valueController;

            if (synth.VerboseController)
            {
                Debug.LogFormat("ChangeController\tChannel:{0}\tControl:{1}\tValue:{2}", channum, numController, valueController);
            }

            switch (numController)
            {
                case MPTKController.Sustain:
                    {
                        if (valueController < 64)
                        {
                            /*  	printf("** sustain off\n"); */
                            synth.fluid_synth_damp_voices(channum);
                        }
                        else
                        {
                            /*  	printf("** sustain on\n"); */
                        }
                    }
                    break;

                case MPTKController.BankSelectMsb:
                    // style == FLUID_BANK_STYLE_GS  
                    banknum = valueController & 0x7F;
                    mptkChannel.lastBank = banknum;
                    synth.fluid_synth_program_change(channum, prognum);
                    break;

                case MPTKController.BankSelectLsb:
                    // Not implemented
                    // FIXME: according to the Downloadable Sounds II specification, bit 31 should be set when we receive the message on channel 10 (drum channel)
                    // MPTK bank style is FLUID_BANK_STYLE_GS (see fluidsynth), bank = CC0/MSB (CC32/LSB ignored)
                    break;

                case MPTKController.AllNotesOff:
                    synth.fluid_synth_noteoff(channum, -1);
                    break;

                case MPTKController.AllSoundOff:
                    synth.fluid_synth_soundoff(channum);
                    break;

                case MPTKController.ResetAllControllers:
                    fluid_channel_init_ctrl();
                    synth.fluid_synth_modulate_voices_all(channum);
                    break;

                case MPTKController.DATA_ENTRY_LSB: /* not allowed to modulate (spec SF 2.01 - 8.2.1) */
                    break;

                case MPTKController.DATA_ENTRY_MSB: /* not allowed to modulate (spec SF 2.01 - 8.2.1) */
                    {
                        //int data = (valueController << 7) + cc[(int)MPTKController.DATA_ENTRY_LSB] ;

                        //if (chan->nrpn_active)   /* NRPN is active? */
                        //{
                        //    /* SontFont 2.01 NRPN Message (Sect. 9.6, p. 74)  */
                        //    if ((fluid_channel_get_cc(chan, NRPN_MSB) == 120)
                        //            && (fluid_channel_get_cc(chan, NRPN_LSB) < 100))
                        //    {
                        //        nrpn_select = chan->nrpn_select;

                        //        if (nrpn_select < GEN_LAST)
                        //        {
                        //            float val = fluid_gen_scale_nrpn(nrpn_select, data);
                        //            fluid_synth_set_gen_LOCAL(synth, channum, nrpn_select, val);
                        //        }

                        //        chan->nrpn_select = 0;  /* Reset to 0 */
                        //    }
                        //}
                        //else 
                        // if (fluid_channel_get_cc(chan, RPN_MSB) == 0)      /* RPN is active: MSB = 0? */
                        {
                            switch ((midi_rpn_event)cc[(int)MPTKController.RPN_LSB])
                            {
                                case midi_rpn_event.RPN_PITCH_BEND_RANGE:    /* Set bend range in semitones */
                                    //fluid_channel_set_pitch_wheel_sensitivity(synth->channel[channum], value);
                                    pitch_wheel_sensitivity = (short)valueController;

                                    /* Update bend range */
                                    /* fluid_synth_update_pitch_wheel_sens_LOCAL(synth, channum);    
                                           fluid_synth_modulate_voices_LOCAL(synth, chan, 0, FLUID_MOD_PITCHWHEELSENS);
                                                fluid_voice_t* voice;
                                                int i;

                                                for (i = 0; i < synth->polyphony; i++)
                                                {
                                                    voice = synth->voice[i];

                                                    if (fluid_voice_get_channel(voice) == chan)
                                                    {
                                                        fluid_voice_modulate(voice, is_cc, ctrl);
                                                    }
                                                }
                                    */
                                    break;

                                    //case RPN_CHANNEL_FINE_TUNE:   /* Fine tune is 14 bit over +/-1 semitone (+/- 100 cents, 8192 = center) */
                                    //    fluid_synth_set_gen_LOCAL(synth, channum, GEN_FINETUNE,
                                    //                              (float)(data - 8192) * (100.0f / 8192.0f));
                                    //    break;

                                    //case RPN_CHANNEL_COARSE_TUNE: /* Coarse tune is 7 bit and in semitones (64 is center) */
                                    //    fluid_synth_set_gen_LOCAL(synth, channum, GEN_COARSETUNE,
                                    //                              value - 64);
                                    //    break;

                                    //case RPN_TUNING_PROGRAM_CHANGE:
                                    //    fluid_channel_set_tuning_prog(chan, value);
                                    //    fluid_synth_activate_tuning(synth, channum,
                                    //                                fluid_channel_get_tuning_bank(chan),
                                    //                                value, TRUE);
                                    //    break;

                                    //case RPN_TUNING_BANK_SELECT:
                                    //    fluid_channel_set_tuning_bank(chan, value);
                                    //    break;

                                    //case RPN_MODULATION_DEPTH_RANGE:
                                    //    break;
                            }
                        }

                        break;
                    }
                //case MPTKController.DATA_ENTRY_MSB:
                //    {
                //        //int data = (value << 7) + chan->cc[DATA_ENTRY_LSB];

                ///* SontFont 2.01 NRPN Message (Sect. 9.6, p. 74)  */
                //if ((chan->cc[NRPN_MSB] == 120) && (chan->cc[NRPN_LSB] < 100))
                //{
                //    float val = fluid_gen_scale_nrpn(chan->nrpn_select, data);
                //    FLUID_LOG(FLUID_WARN, "%s: %d: Data = %d, value = %f", __FILE__, __LINE__, data, val);
                //    fluid_synth_set_gen(chan->synth, chan->channum, chan->nrpn_select, val);
                //}
                //    break;
                //}

                //case MPTKController.NRPN_MSB:
                //    cc[(int)MPTKController.NRPN_LSB] = 0;
                //    nrpn_select = 0;
                //    break;

                //case MPTKController.NRPN_LSB:
                //    /* SontFont 2.01 NRPN Message (Sect. 9.6, p. 74)  */
                //    if (cc[(int)MPTKController.NRPN_MSB] == 120)
                //    {
                //        if (value == 100)
                //        {
                //            nrpn_select += 100;
                //        }
                //        else if (value == 101)
                //        {
                //            nrpn_select += 1000;
                //        }
                //        else if (value == 102)
                //        {
                //            nrpn_select += 10000;
                //        }
                //        else if (value < 100)
                //        {
                //            nrpn_select += value;
                //            Debug.LogWarning(string.Format("NRPN Select = {0}", nrpn_select));
                //        }
                //    }
                //    break;

                //case MPTKController.RPN_MSB:
                //    break;

                //case MPTKController.RPN_LSB:
                //    // erase any previously received NRPN message 
                //    cc[(int)MPTKController.NRPN_MSB] = 0;
                //    cc[(int)MPTKController.NRPN_LSB] = 0;
                //    nrpn_select = 0;
                //    break;

                default:
                    if (synth.MPTK_ApplyRealTimeModulator)
                        synth.fluid_synth_modulate_voices(channum, 1, (int)numController);
                    break;
            }
        }


        /*
         * fluid_channel_pitch_bend
         */
        public void fluid_channel_pitch_bend(int val)
        {
            if (synth.VerboseController)
            {
                Debug.LogFormat("PitchChange\tChannel:{0}\tValue:{1}", channum, val);
            }
            pitch_bend = (short)val;
            synth.fluid_synth_modulate_voices(channum, 0, (int)fluid_mod_src.FLUID_MOD_PITCHWHEEL); //STRANGE
        }
    }
}
