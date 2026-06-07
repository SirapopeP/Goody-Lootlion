import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-child-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslocoPipe],
  templateUrl: './child-bottom-nav.component.html',
  styleUrl: './child-bottom-nav.component.scss',
})
export class ChildBottomNavComponent {}
