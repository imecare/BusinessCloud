---
mode: agent
description: Implementa cambios end-to-end en el proyecto Front bajo estándares enterprise.
---

Implementa la solicitud del usuario de extremo a extremo en el proyecto **Front**, respetando los estándares definidos en [copilot-instructions.md](/D:/PROYECTO_MASTER/BazarGeneralRepo/Front/.github/copilot-instructions.md).

## Alcance obligatorio
- Entregar implementación completa (no solo propuesta).
- Incluir cambios en todos los archivos necesarios.
- Verificar build/type-check/tests/lint disponibles antes de cerrar.
- Reportar archivos modificados, validaciones ejecutadas y resultado.

## Reglas técnicas
- Angular moderno standalone (`loadComponent`), sin `NgModule`.
- Patrón Smart/Dumb components.
- Estado con Signals y `@ngrx/signals` cuando aplique.
- Tipado estricto, sin `any`.
- Formularios con Reactive Forms tipados.
- RxJS para asincronía y prevención de fugas (`takeUntilDestroyed`/`async`).

## Seguridad y multi-tenant
- Nunca usar `localStorage`/`sessionStorage` para JWT sensible.
- Mantener/crear guards funcionales multi-tenant validando `tenantId` contra claims.
- Mantener/crear interceptor funcional para identidad de tenant y auth.
- No silenciar errores: manejo explícito + notificación al usuario.

## UX y calidad
- Tailwind v4 mobile-first.
- Accesibilidad WCAG 2.1 AA.
- Soporte i18n (fechas, números, moneda).
- Prohibido truncar archivos o dejar placeholders.

Si falta una decisión de negocio, pregunta una sola cosa puntual y luego continúa.
