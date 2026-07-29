import React from 'react';
import {Composition} from 'remotion';
import {BazarPromo, type BazarPromoProps} from './video';
import {BazarTutorial} from './tutorial';
import {BazarExtendedTutorial, extendedTutorialDuration} from './extended-tutorial';

const defaultProps: BazarPromoProps = {
  headline: 'Haz crecer tu bazar',
  subtitle: 'Controla ventas, cobros, clientes y entregas desde un solo lugar.',
  callToAction: 'Conoce Bazar-Enlace',
};

export const VideoRoot: React.FC = () => (
  <>
    <Composition
      id="BazarPromo"
      component={BazarPromo}
      durationInFrames={540}
      fps={30}
      width={1080}
      height={1920}
      defaultProps={defaultProps}
    />
    <Composition
      id="BazarTutorial"
      component={BazarTutorial}
      durationInFrames={900}
      fps={30}
      width={1080}
      height={1920}
      defaultProps={defaultProps}
    />
    <Composition
      id="BazarExtendedTutorial"
      component={BazarExtendedTutorial}
      durationInFrames={extendedTutorialDuration}
      fps={30}
      width={1080}
      height={1920}
      defaultProps={{
        appName: 'Bazar-Enlace',
        primaryColor: '#5745f5',
        accentColor: '#18b7dd',
      }}
    />
  </>
);
