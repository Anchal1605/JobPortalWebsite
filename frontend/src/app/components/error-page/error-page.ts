import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './error-page.html',
  styleUrl: './error-page.css',
})
export class ErrorPage {

}
