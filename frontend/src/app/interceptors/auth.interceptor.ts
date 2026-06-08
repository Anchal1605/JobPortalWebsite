import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const token = localStorage.getItem('token');

    //public auth routes are not intercepted (we don't want to add token to them)
    const url = req.url.toLowerCase();
    const isAuthRoute = url.includes('/api/auth/login') || url.includes('/api/auth/register');

    let outgoing = req;
    if (token && !isAuthRoute) {
        //since HTTP request are immutable, we need to clone the request and add the token to the headers
        outgoing = req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        })
    }

    return next(outgoing).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401 && !isAuthRoute) {
                localStorage.removeItem('token');
                router.navigate(['/auth/login']);
            }
            return throwError(() => error);
        })
    );
}