import React from 'react';
import {
  AbsoluteFill,
  Img,
  interpolate,
  Sequence,
  spring,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';
import type {BazarPromoProps} from './video';

const colors = {
  ink: '#111827',
  muted: '#64748b',
  indigo: '#4f46e5',
  violet: '#7c3aed',
  cyan: '#06b6d4',
};

const enter = (frame: number, fps: number, delay = 0) => {
  const progress = spring({
    frame: frame - delay,
    fps,
    config: {damping: 16, stiffness: 115, mass: 0.8},
  });
  return {
    opacity: progress,
    transform: `translateY(${interpolate(progress, [0, 1], [55, 0])}px)`,
  };
};

const Background: React.FC = () => {
  const frame = useCurrentFrame();
  const drift = Math.sin(frame / 38) * 30;
  return (
    <AbsoluteFill style={{background: 'linear-gradient(155deg,#f8fafc 0%,#eef2ff 52%,#ecfeff 100%)'}}>
      <div style={{
        position: 'absolute',
        width: 760,
        height: 760,
        right: -320 + drift,
        top: -330,
        borderRadius: '50%',
        background: 'radial-gradient(circle,rgba(124,58,237,.2),rgba(124,58,237,0) 70%)',
      }} />
      <div style={{
        position: 'absolute',
        width: 800,
        height: 800,
        left: -370 - drift,
        bottom: -390,
        borderRadius: '50%',
        background: 'radial-gradient(circle,rgba(6,182,212,.18),rgba(6,182,212,0) 70%)',
      }} />
    </AbsoluteFill>
  );
};

const Logo: React.FC = () => (
  <div style={{display: 'flex', alignItems: 'center', gap: 18}}>
    <div style={{
      width: 74,
      height: 74,
      borderRadius: 22,
      display: 'grid',
      placeItems: 'center',
      color: '#fff',
      fontSize: 30,
      fontWeight: 900,
      background: `linear-gradient(135deg,${colors.indigo},${colors.violet})`,
      boxShadow: '0 16px 35px rgba(79,70,229,.3)',
    }}>BE</div>
    <div>
      <div style={{fontSize: 35, color: colors.ink, fontWeight: 900}}>Bazar-Enlace</div>
      <div style={{fontSize: 20, color: colors.muted, fontWeight: 650}}>Control de ventas inteligente</div>
    </div>
  </div>
);

const Intro: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  return (
    <AbsoluteFill style={{padding: '155px 88px', justifyContent: 'space-between'}}>
      <div style={enter(frame, fps)}><Logo /></div>
      <div>
        <div style={{...enter(frame, fps, 12), color: colors.indigo, fontSize: 28, fontWeight: 900, letterSpacing: 4, textTransform: 'uppercase'}}>Tutorial con el sistema real</div>
        <div style={{...enter(frame, fps, 22), marginTop: 30, color: colors.ink, fontSize: 103, lineHeight: 1.02, fontWeight: 950, letterSpacing: -5}}>
          Administra tu bazar,<br /><span style={{color: colors.indigo}}>paso a paso.</span>
        </div>
        <div style={{...enter(frame, fps, 38), marginTop: 52, color: colors.muted, fontSize: 35, lineHeight: 1.35, fontWeight: 650}}>
          Conoce las funciones principales directamente en Bazar-Enlace.
        </div>
      </div>
      <div style={{...enter(frame, fps, 48), width: 150, height: 10, borderRadius: 8, background: `linear-gradient(90deg,${colors.indigo},${colors.cyan})`}} />
    </AbsoluteFill>
  );
};

type ScreenSceneProps = {
  step: string;
  title: string;
  description: string;
  image: string;
  callout: string;
  focusX: number;
  focusY: number;
};

const ScreenScene: React.FC<ScreenSceneProps> = ({
  step,
  title,
  description,
  image,
  callout,
  focusX,
  focusY,
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const zoom = interpolate(frame, [35, 155], [1, 1.12], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const translateX = interpolate(frame, [35, 155], [0, focusX], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const translateY = interpolate(frame, [35, 155], [0, focusY], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const pulse = 1 + Math.sin(frame / 8) * 0.025;

  return (
    <AbsoluteFill style={{padding: '82px 64px'}}>
      <div style={enter(frame, fps)}>
        <div style={{color: colors.indigo, fontSize: 25, fontWeight: 900, letterSpacing: 3, textTransform: 'uppercase'}}>{step}</div>
        <div style={{marginTop: 12, color: colors.ink, fontSize: 63, lineHeight: 1.06, fontWeight: 950, letterSpacing: -2}}>{title}</div>
        <div style={{marginTop: 14, color: colors.muted, fontSize: 27, lineHeight: 1.35, fontWeight: 650}}>{description}</div>
      </div>

      <div style={{
        ...enter(frame, fps, 10),
        position: 'relative',
        height: 770,
        marginTop: 52,
        overflow: 'hidden',
        border: '5px solid #fff',
        borderRadius: 34,
        background: '#fff',
        boxShadow: '0 35px 90px rgba(15,23,42,.2)',
      }}>
        <Img
          src={staticFile(`screens/${image}`)}
          style={{
            position: 'absolute',
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            objectPosition: 'center top',
            transform: `translate(${translateX}px, ${translateY}px) scale(${zoom})`,
          }}
        />
        <div style={{
          position: 'absolute',
          left: 34,
          bottom: 32,
          maxWidth: 690,
          padding: '22px 30px',
          borderRadius: 20,
          color: '#fff',
          background: 'rgba(17,24,39,.9)',
          boxShadow: '0 16px 40px rgba(0,0,0,.24)',
          fontSize: 25,
          lineHeight: 1.35,
          fontWeight: 800,
          transform: `scale(${pulse})`,
          transformOrigin: 'left bottom',
        }}>
          {callout}
        </div>
      </div>

      <div style={{...enter(frame, fps, 22), display: 'flex', alignItems: 'center', gap: 18, marginTop: 48}}>
        <div style={{width: 72, height: 72, borderRadius: '50%', display: 'grid', placeItems: 'center', color: '#fff', background: colors.indigo, fontSize: 34, fontWeight: 950}}>✓</div>
        <div style={{color: colors.ink, fontSize: 30, fontWeight: 900}}>Función integrada en tu flujo diario</div>
      </div>
    </AbsoluteFill>
  );
};

const Outro: React.FC<BazarPromoProps> = ({callToAction}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  return (
    <AbsoluteFill style={{padding: '170px 86px', justifyContent: 'center', alignItems: 'center', textAlign: 'center'}}>
      <div style={enter(frame, fps)}><Logo /></div>
      <div style={{...enter(frame, fps, 10), marginTop: 100, color: colors.ink, fontSize: 88, lineHeight: 1.06, fontWeight: 950, letterSpacing: -4}}>
        Vende, cobra y entrega<br /><span style={{color: colors.indigo}}>con mayor control.</span>
      </div>
      <div style={{
        ...enter(frame, fps, 20),
        marginTop: 75,
        minWidth: 700,
        padding: '32px 50px',
        borderRadius: 999,
        color: '#fff',
        background: `linear-gradient(135deg,${colors.indigo},${colors.violet})`,
        boxShadow: '0 28px 65px rgba(79,70,229,.38)',
        fontSize: 40,
        fontWeight: 900,
      }}>{callToAction}</div>
    </AbsoluteFill>
  );
};

export const BazarTutorial: React.FC<BazarPromoProps> = (props) => (
  <AbsoluteFill style={{fontFamily: 'Arial, sans-serif'}}>
    <Background />
    <Sequence from={0} durationInFrames={120} premountFor={30}><Intro /></Sequence>
    <Sequence from={120} durationInFrames={180} premountFor={30}>
      <ScreenScene
        step="1. Revisa tu operación"
        title="Consulta el Dashboard"
        description="Visualiza cartera, cobros, pendientes y desempeño por recolector."
        image="dashboard.png"
        callout="Selecciona Hoy, Semana o Mes para analizar tus resultados."
        focusX={-28}
        focusY={42}
      />
    </Sequence>
    <Sequence from={300} durationInFrames={180} premountFor={30}>
      <ScreenScene
        step="2. Captura una venta"
        title="Registra productos rápidamente"
        description="Selecciona el evento y cliente; después escribe descripción y precio."
        image="sales.png"
        callout="Presiona Agregar para sumar cada producto a la venta."
        focusX={-35}
        focusY={30}
      />
    </Sequence>
    <Sequence from={480} durationInFrames={150} premountFor={30}>
      <ScreenScene
        step="3. Confirma los pagos"
        title="Valida comprobantes"
        description="Identifica cierres abiertos y revisa cuántos pagos siguen pendientes."
        image="proofs.png"
        callout="Abre un cierre para revisar y autorizar sus comprobantes."
        focusX={-35}
        focusY={55}
      />
    </Sequence>
    <Sequence from={630} durationInFrames={150} premountFor={30}>
      <ScreenScene
        step="4. Prepara la entrega"
        title="Organiza la logística"
        description="Consulta eventos, fechas de entrega y paquetes listos para despacho."
        image="logistics.png"
        callout="Selecciona un cierre para generar etiquetas y hoja de despacho."
        focusX={-32}
        focusY={50}
      />
    </Sequence>
    <Sequence from={780} durationInFrames={120} premountFor={30}><Outro {...props} /></Sequence>
  </AbsoluteFill>
);
