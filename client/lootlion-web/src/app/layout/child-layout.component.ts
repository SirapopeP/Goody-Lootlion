import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChildBottomNavComponent } from './child-bottom-nav.component';

@Component({
  selector: 'app-child-layout',
  standalone: true,
  imports: [RouterOutlet, ChildBottomNavComponent],
  templateUrl: './child-layout.component.html',
  styleUrl: './child-layout.component.scss',
})
export class ChildLayoutComponent {}
