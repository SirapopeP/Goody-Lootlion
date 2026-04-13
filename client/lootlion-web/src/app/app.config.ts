import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, isDevMode } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { provideTransloco } from '@jsverse/transloco';
import { provideTaiga } from '@taiga-ui/core';
import { provideApi } from './api/generated/provide-api';
import { environment } from '../environments/environment';
import { authInterceptor } from './core/auth/auth.interceptor';
import { LocaleSyncService } from './core/i18n/locale-sync.service';
import { TranslocoHttpLoader } from './core/i18n/transloco-http.loader';
import { routes } from './app.routes';

function initLocaleSync(_sync: LocaleSyncService): () => Promise<void> {
  return () => Promise.resolve();
}

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: initLocaleSync,
      deps: [LocaleSyncService],
    },
    provideAnimations(),
    provideTaiga({ mode: 'dark' }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideTransloco({
      config: {
        availableLangs: ['th', 'en'],
        defaultLang: 'th',
        fallbackLang: 'th',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    provideApi(environment.apiBaseUrl),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
