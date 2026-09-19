using System;
using UnityEngine;

namespace VeilHouse
{
    /// <summary>Deterministic original acoustic sketches. Signals are band-limited by simple lowpass
    /// layers, attack/release envelopes, conservative gain and a soft saturation stage.</summary>
    internal sealed class SoundSynthesis
    {
        internal enum Kind { Room,Rain,Unease,Footstep,Door,Creak,Thud,Throw,Rattle,Light,Television,Radio,Water,Pulse,Knock,Blackout,UI,Reveal }
        const int Rate=32000;
        const double Tau=Math.PI*2;
        static readonly double[] RadioNotes={196.0,233.08,261.63,220.0,174.61};
        uint random=0xD09FE123;
        double low,slow,mid;
        double Noise()
        {
            random^=random<<13;random^=random>>17;random^=random<<5;
            return random/(double)uint.MaxValue*2-1;
        }
        static double Sin(double frequency,double time) {return Math.Sin(Tau*frequency*time);}
        static double Decay(double time,double speed) {return time<0?0:Math.Exp(-time*speed);}
        static double Smooth(double value) {value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value);}
        static double Bell(double t,double frequency,double decay)
        {
            return (Sin(frequency,t)+.35*Sin(frequency*2.012,t)+.13*Sin(frequency*3.94,t))*Decay(t,decay);
        }
        internal AudioClip Generate(string name,float seconds,Kind kind,bool looping=false)
        {
            int count=(int)(seconds*Rate);var data=new float[count];low=slow=mid=0;
            double phase=0;
            for(int i=0;i<count;i++) {
                double t=(double)i/Rate,n=Noise();
                low+=(n-low)*.075;slow+=(n-slow)*.004;mid+=(n-mid)*.31;
                double band=mid-low,s=0;
                switch(kind) {
                    case Kind.Room:
                        s=low*.19+slow*.32+.009*Sin(49,t)+.008*Sin(67,t)+.011*Sin(29,t)*(1+.4*Sin(.25,t));
                        break;
                    case Kind.Rain:
                        s=band*.35+low*.15;
                        s*=.72+.15*Sin(.2,t)+.08*Sin(.7,t);
                        s+=Math.Max(0,Sin(13.7,t))*.011*Sin(1200+50*Sin(2,t),t);
                        break;
                    case Kind.Unease:
                        s=.17*Sin(41,t)+.085*Sin(43,t)+.045*Sin(61,t)+low*.13;
                        s*=.65+.25*Sin(.1666666667,t);
                        break;
                    case Kind.Footstep:
                        s=.38*Sin(105-90*t,t)*Decay(t,28)+low*1.35*Decay(t,27)+band*.28*Decay(t,65);
                        if(t>.065)s+=low*.26*Decay(t-.065,33);
                        break;
                    case Kind.Door:
                        phase+=Tau*(155+67*Math.Sin(t*4)+23*Math.Sin(t*27))/Rate;
                        s=(Math.Sin(phase)+.28*Math.Sin(phase*2.08)+low*.9)*.17*Smooth(t*12)*Smooth((seconds-t)*5);
                        if(t>.83)s+=.23*Sin(105,t-.83)*Decay(t-.83,36)+band*.38*Decay(t-.83,48);
                        break;
                    case Kind.Creak:
                        phase+=Tau*(112+40*Math.Sin(t*2.3)+5*Math.Sin(t*50))/Rate;
                        s=.14*(Math.Sin(phase)+.2*Math.Sin(phase*2.97)+low)*Smooth(t*3)*Smooth((seconds-t)*3);
                        break;
                    case Kind.Thud:
                        s=.53*Sin(96-80*t,t)*Decay(t,16)+low*1.8*Decay(t,23)+band*.48*Decay(t,60);
                        break;
                    case Kind.Throw:
                        s=low*.70*Math.Sin(Math.PI*t/seconds)+band*.25*Math.Sin(Math.PI*t/seconds);
                        s+=.055*Sin(96-80*t,t)*Smooth(t*10)*Smooth((seconds-t)*10);
                        break;
                    case Kind.Rattle:
                        for(int j=0;j<6;j++) {
                            double hit=t-j*.119;
                            if(hit>=0)s+=(low*.55+band*.26+Sin(880+j*79,hit)*.05+Sin(1490-j*23,hit)*.035)*Decay(hit,42);
                        }
                        break;
                    case Kind.Light:
                        s=band*.7*Decay(t,110)+low*.4*Decay(t,55);
                        if(t>.13)s+=(.03*Sin(100,t)+band*.16)*Decay(t-.13,14);
                        break;
                    case Kind.Television:
                        s=(band*.7+low*.17+.025*Sin(100,t))*(.35+.5*Smooth(Math.Sin(t*33)*2));
                        s+=.026*Sin(781,t)*Math.Pow(Math.Max(0,Sin(3,t)),4);
                        s*=Smooth(t*18)*Smooth((seconds-t)*6);
                        break;
                    case Kind.Radio:
                        int note=(int)(t*3.5)%5;double f=RadioNotes[note];
                        s=(.072*Sin(f,t)+.022*Sin(f*2.008,t)+band*.38)*( .35+.5*Math.Pow(Math.Max(0,Sin(2.3,t)),2));
                        s*=Smooth(t*12)*Smooth((seconds-t)*7);
                        break;
                    case Kind.Water:
                        s=(band*.63+low*.30)*(.65+.19*Sin(8.3,t)+.09*Sin(27,t));
                        s+=.032*Sin(560+100*Sin(2,t),t)*Math.Pow(Math.Max(0,Sin(7.1,t)),6);
                        s*=Smooth(t*9)*Smooth((seconds-t)*7);
                        break;
                    case Kind.Pulse:
                        phase+=Tau*(91-60*Math.Min(t,1.6)/1.6)/Rate;
                        s=(Math.Sin(phase)*.36+Math.Sin(phase*.505)*.14+low*.61+band*.08)*Smooth(t*8)*Math.Exp(-t*1.8);
                        s+=.09*Sin(207,t)*Smooth(t*3)*Math.Exp(-t*2.8);
                        break;
                    case Kind.Knock:
                        for(int j=0;j<3;j++) {
                            double hit=t-j*.36;
                            if(hit>=0)s+=(.32*Sin(156,hit)+.12*Sin(321,hit)+low*.69)*Decay(hit,27);
                        }
                        break;
                    case Kind.Blackout:
                        s=(band*.44+.10*Sin(100,t))*(.2+.8*Math.Pow(Math.Max(0,Sin(13,t)),4))*Smooth(t*20)*Smooth((.7-t)*4);
                        if(t>.49)s+=(.28*Sin(39,t-.49)+low*.23)*Decay(t-.49,3)*Smooth((t-.49)*16);
                        break;
                    case Kind.UI:
                        s=band*.15*Decay(t,34)+.085*Bell(t,698.46,13);
                        if(t>.065)s+=.065*Bell(t-.065,932.32,17);
                        break;
                    case Kind.Reveal:
                        s=.18*Bell(t,220,1.9)+.09*Bell(t,329.63,2.2);
                        if(t>.3)s+=.13*Bell(t-.3,440,2.4);
                        if(t>.6)s+=.08*Bell(t-.6,523.25,2.2);
                        s+=low*.035*Smooth((seconds-t)*2);
                        break;
                }
                if(!looping)s*=Smooth(t/.004)*Smooth((seconds-t)/.018);
                data[i]=(float)(Math.Tanh(s*1.15)*.8);
            }
            if(looping) {
                // Crossfade the final half second into the opening. Equal endpoints prevent loop clicks.
                int fade=Rate/2;
                for(int i=0;i<fade;i++) {
                    float mix=(float)Smooth((double)i/(fade-1));
                    data[count-fade+i]=Mathf.Lerp(data[count-fade+i],data[i],mix);
                }
                // The last endpoint is the first sample. A short smoothing window ensures a quiet seam.
                float last=data[count-1],first=data[0];
                for(int i=0;i<128;i++)data[count-128+i]+= (first-last)*(i/127f);
            }
            var clip=AudioClip.Create(name,count,1,Rate,false);
            clip.SetData(data,0);return clip;
        }
    }
}

