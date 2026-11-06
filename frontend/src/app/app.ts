import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './components/navbar/navbar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar],
  templateUrl: './app.html',
  styles: [`
    .container {
      max-width: 900px;
      margin: 30px auto;
      padding: 0 16px;
      font-family: system-ui, -apple-system, sans-serif;
    }
  `]
})
export class AppComponent {}