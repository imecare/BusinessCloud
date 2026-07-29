import React from 'react';
import {
  AbsoluteFill,
  Easing,
  interpolate,
  Sequence,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export type BazarPromoProps = {
  headline: string;
  subtitle: string;
  callToAction: string;
};

const colors = {
  ink: '#111827',
  muted: '#64748b',
  indigo: '#4f46e5',
  violet: '#7c3aed',
  cyan: '#06b6d4',
  white: '#ffffff',
};

const rise = (frame: number, fps: number, delay = 0) => {
  const progress = spring({
    frame: frame - delay,
    fps,
    config: {damping: 14, stiffness: 110, mass: 0.8},
  });
  return {
    opacity: progress,
    transform: `translateY(${interpolate(progress, [0, 1], [70, 0])}px) scale(${interpolate(progress, [0, 1], [0.94, 1])})`,
  };
};

const Icon: React.FC<{kind: 'sale' | 'payment' | 'client' | 'delivery'}> = ({kind}) => {
  const paths = {
    sale: <><path d="M12 3v18M17 7H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H7" /></>,
    payment: <><circle cx="12" cy="12" r="9" /><path d="m8 12 2.7 2.7L16.5 9" /></>,
    client: <><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4.2 3.6-7 8-7s8 2.8 8 7" /></>,
    delivery: <><path d="M3 6h11v11H3zM14 10h4l3 3v4h-7z" /><circle cx="7" cy="19" r="2" /><circle cx="18" cy="19" r="2" /></>,
  };
  return (
    <svg width="58" height="58" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {paths[kind]}
    </svg>
  );
};

const Brand: React.FC = () => (
  <div style={{display: 'flex', alignItems: 'center', gap: 22}}>
    <div style={{
      width: 86,
      height: 86,
      borderRadius: 26,
      display: 'grid',
      placeItems: 'center',
      color: colors.white,
      fontSize: 38,
      fontWeight: 900,
      background: `linear-gradient(135deg, ${colors.indigo}, ${colors.violet})`,
      boxShadow: '0 20px 45px rgba(79,70,229,.34)',
    }}>BE</div>
    <div>
      <div style={{fontSize: 42, fontWeight: 900, color: colors.ink}}>Bazar-Enlace</div>
      <div style={{fontSize: 23, fontWeight: 600, color: colors.muted}}>Control de ventas inteligente</div>
    </div>
  </div>
);

const Background: React.FC = () => {
  const frame = useCurrentFrame();
  const drift = Math.sin(frame / 34) * 35;
  return (
    <AbsoluteFill style={{background: 'linear-gradient(160deg, #f8fafc 0%, #eef2ff 46%, #ecfeff 100%)'}}>
      <div style={{
        position: 'absolute',
        width: 780,
        height: 780,
        borderRadius: '50%',
        right: -330 + drift,
        top: -240,
        background: 'radial-gradient(circle, rgba(124,58,237,.22), rgba(124,58,237,0) 70%)',
      }} />
      <div style={{
        position: 'absolute',
        width: 900,
        height: 900,
        borderRadius: '50%',
        left: -450 - drift,
        bottom: -260,
        background: 'radial-gradient(circle, rgba(6,182,212,.22), rgba(6,182,212,0) 70%)',
      }} />
    </AbsoluteFill>
  );
};

const Intro: React.FC<BazarPromoProps> = ({headline}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  return (
    <AbsoluteFill style={{padding: '150px 92px', justifyContent: 'space-between'}}>
      <div style={rise(frame, fps, 0)}><Brand /></div>
      <div>
        <div style={{...rise(frame, fps, 12), color: colors.indigo, fontSize: 30, fontWeight: 800, letterSpacing: 4, textTransform: 'uppercase'}}>
          Menos tareas. Mas control.
        </div>
        <div style={{...rise(frame, fps, 22), marginTop: 34, color: colors.ink, fontSize: 112, lineHeight: 1.02, fontWeight: 950, letterSpacing: -5}}>
          {headline}
        </div>
      </div>
      <div style={{...rise(frame, fps, 38), width: 140, height: 10, borderRadius: 9, background: `linear-gradient(90deg, ${colors.indigo}, ${colors.cyan})`}} />
    </AbsoluteFill>
  );
};

const Features: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const features = [
    {title: 'Ventas', copy: 'Captura rapida por evento', kind: 'sale' as const, color: colors.indigo},
    {title: 'Cobros', copy: 'Saldos y comprobantes', kind: 'payment' as const, color: '#059669'},
    {title: 'Clientes', copy: 'Historial siempre disponible', kind: 'client' as const, color: colors.violet},
    {title: 'Entregas', copy: 'Logistica bajo control', kind: 'delivery' as const, color: '#ea580c'},
  ];
  return (
    <AbsoluteFill style={{padding: '150px 86px'}}>
      <div style={{...rise(frame, fps), color: colors.ink, fontSize: 72, lineHeight: 1.08, fontWeight: 950}}>
        Todo tu bazar,<br /><span style={{color: colors.indigo}}>en un solo lugar.</span>
      </div>
      <div style={{display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 30, marginTop: 90}}>
        {features.map((feature, index) => (
          <div key={feature.title} style={{
            ...rise(frame, fps, 10 + index * 7),
            minHeight: 300,
            borderRadius: 36,
            padding: 42,
            background: 'rgba(255,255,255,.9)',
            border: '2px solid rgba(255,255,255,.95)',
            boxShadow: '0 24px 70px rgba(15,23,42,.10)',
          }}>
            <div style={{color: feature.color}}><Icon kind={feature.kind} /></div>
            <div style={{marginTop: 32, color: colors.ink, fontSize: 46, fontWeight: 900}}>{feature.title}</div>
            <div style={{marginTop: 12, color: colors.muted, fontSize: 27, lineHeight: 1.35, fontWeight: 600}}>{feature.copy}</div>
          </div>
        ))}
      </div>
    </AbsoluteFill>
  );
};

const Message: React.FC<BazarPromoProps> = ({subtitle}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const value = interpolate(frame, [0, 95], [0, 94], {extrapolateRight: 'clamp', easing: Easing.out(Easing.cubic)});
  return (
    <AbsoluteFill style={{padding: '150px 90px', justifyContent: 'center'}}>
      <div style={rise(frame, fps)}>
        <div style={{color: colors.muted, fontSize: 28, fontWeight: 800, letterSpacing: 4, textTransform: 'uppercase'}}>Tu negocio en tiempo real</div>
        <div style={{marginTop: 35, color: colors.ink, fontSize: 78, lineHeight: 1.15, fontWeight: 950, letterSpacing: -2}}>{subtitle}</div>
      </div>
      <div style={{...rise(frame, fps, 18), marginTop: 95, padding: 48, borderRadius: 38, background: colors.white, boxShadow: '0 30px 80px rgba(15,23,42,.12)'}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'end'}}>
          <div>
            <div style={{fontSize: 27, color: colors.muted, fontWeight: 700}}>Recuperacion de cartera</div>
            <div style={{marginTop: 10, fontSize: 88, color: colors.indigo, fontWeight: 950}}>{Math.round(value)}%</div>
          </div>
          <div style={{width: 190, height: 190, borderRadius: '50%', display: 'grid', placeItems: 'center', color: colors.indigo, background: 'linear-gradient(145deg,#eef2ff,#e0e7ff)', fontSize: 82, fontWeight: 900}}>✓</div>
        </div>
      </div>
    </AbsoluteFill>
  );
};

const Outro: React.FC<BazarPromoProps> = ({callToAction}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const pulse = 1 + Math.sin(frame / 8) * 0.025;
  return (
    <AbsoluteFill style={{padding: '170px 86px', justifyContent: 'center', alignItems: 'center', textAlign: 'center'}}>
      <div style={rise(frame, fps)}><Brand /></div>
      <div style={{...rise(frame, fps, 12), marginTop: 130, color: colors.ink, fontSize: 92, lineHeight: 1.05, fontWeight: 950, letterSpacing: -4}}>
        Vende. Cobra.<br /><span style={{color: colors.indigo}}>Entrega mejor.</span>
      </div>
      <div style={{
        ...rise(frame, fps, 25),
        transform: `${rise(frame, fps, 25).transform} scale(${pulse})`,
        marginTop: 100,
        minWidth: 700,
        padding: '34px 55px',
        borderRadius: 999,
        color: colors.white,
        background: `linear-gradient(135deg, ${colors.indigo}, ${colors.violet})`,
        boxShadow: '0 28px 65px rgba(79,70,229,.38)',
        fontSize: 42,
        fontWeight: 900,
      }}>{callToAction}</div>
    </AbsoluteFill>
  );
};

export const BazarPromo: React.FC<BazarPromoProps> = (props) => (
  <AbsoluteFill style={{fontFamily: 'Arial, sans-serif'}}>
    <Background />
    <Sequence from={0} durationInFrames={125} premountFor={30}><Intro {...props} /></Sequence>
    <Sequence from={125} durationInFrames={170} premountFor={30}><Features /></Sequence>
    <Sequence from={295} durationInFrames={145} premountFor={30}><Message {...props} /></Sequence>
    <Sequence from={440} durationInFrames={100} premountFor={30}><Outro {...props} /></Sequence>
  </AbsoluteFill>
);
