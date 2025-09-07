// src/app/services/message.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MessageService {
  private apiUrl = 'http://localhost:5293/api/messages';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      Authorization: token ? `Bearer ${token}` : ''
    });
  }

  // send by user id (body must match backend MessageDto { messageContent: string })
  sendToId(receiverId: number, messageContent: string): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/${receiverId}`,
      { messageContent },
      { headers: this.getAuthHeaders().set('Content-Type', 'application/json') }
    );
  }

  // send by username
  sendToUsername(username: string, messageContent: string): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/to/${encodeURIComponent(username)}`,
      { messageContent },
      { headers: this.getAuthHeaders().set('Content-Type', 'application/json') }
    );
  }

  // get conversation by user id -> backend expects /withUser/{id}
  getMessagesWithId(otherUserId: number): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/withUser/${otherUserId}`,
      { headers: this.getAuthHeaders() }
    );
  }

  // get conversation by username -> backend expects /with/username/{username}
  getMessagesWithUsername(username: string): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/with/username/${encodeURIComponent(username)}`,
      { headers: this.getAuthHeaders() }
    );
  }
}
