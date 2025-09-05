import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = 'http://localhost:5293/api';

  constructor(private http: HttpClient) {}

  // Posts
getApprovedPosts(): Observable<any[]> {
  return this.http.get<any[]>(
    `${this.apiUrl}/posts/approved`,
    { headers: this.getAuthHeaders() }
  );
}
  createPost(formData: FormData): Observable<any> {
    return this.http.post(`${this.apiUrl}/posts`, formData);
  }

  likePost(id: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/posts/${id}/like`, {});
  }

 commentPost(postId: number, text: string): Observable<any> {
  return this.http.post(`${this.apiUrl}/posts/${postId}/comment`, { text }, { headers: this.getAuthHeaders() });
}

private getAuthHeaders(): HttpHeaders {
  const token = localStorage.getItem('token');
  return new HttpHeaders({
    'Authorization': `Bearer ${token}`
  });
}

getComments(id: number): Observable<any[]> {
  return this.http.get<any[]>(
    `${this.apiUrl}/posts/${id}/comments`
  );
}

searchUsers(query: string): Observable<any[]> {
  return this.http.get<any[]>(
    `${this.apiUrl}/profiles/search`,
    { params: { query } }
  );
}

reportUser(reportedUserId: number, reason: string) {
  return this.http.post<{ message: string }>(
    `${this.apiUrl}/reports/${reportedUserId}`,
    { reason },
    { headers: this.getAuthHeaders().set('Content-Type', 'application/json') }
  );
}


// --- PROFILE ---
getMyProfile() {
  return this.http.get(`${this.apiUrl}/profiles/me`, {
    headers: this.getAuthHeaders()
  });
}

createProfile(profile: { fullName: string; email?: string; phone?: string; bio?: string }) {
  return this.http.post(`${this.apiUrl}/profiles`, profile, {
    headers: this.getAuthHeaders().set('Content-Type', 'application/json')
  });
}

updateMyProfile(profile: { fullName: string; email?: string; phone?: string; bio?: string }) {
  return this.http.put(`${this.apiUrl}/profiles/me`, profile, {
    headers: this.getAuthHeaders().set('Content-Type', 'application/json')
  });
}

uploadProfileImage(file: File) {
  const form = new FormData();
  form.append('image', file);
  return this.http.post(`${this.apiUrl}/profiles/me/upload-image`, form, {
    headers: this.getAuthHeaders()
  });
}


// ADMIN: Reports
getAllReports() {
  return this.http.get<any[]>(
    `${this.apiUrl}/reports`,
    { headers: this.getAuthHeaders() } // Admin only
  );
}



// ADMIN: list all then filter pending (matches your existing /all)
getPendingPosts() {
  return this.http.get<any[]>(
    `${this.apiUrl}/posts/all`,
    { headers: this.getAuthHeaders() }
  );
}

approvePost(id: number) {
  return this.http.put(
    `${this.apiUrl}/posts/approve/${id}`, // PUT + path matches controller
    {},
    { headers: this.getAuthHeaders() }
  );
}

rejectPost(id: number) {
  return this.http.put(
    `${this.apiUrl}/posts/reject/${id}`,   // PUT + path matches controller
    {},
    { headers: this.getAuthHeaders() }
  );
}


// Now starting user management methods

// --- ADMIN: Users ---
getUsers() {
  return this.http.get<any[]>(
    `${this.apiUrl}/users`,
    { headers: this.getAuthHeaders() }
  );
}

updateUser(id: number, payload: { username: string; role: string; newPassword?: string }) {
  return this.http.put(
    `${this.apiUrl}/users/${id}`,
    payload,
    { headers: this.getAuthHeaders() }
  );
}


deleteUser(id: number) {
  return this.http.delete(
    `${this.apiUrl}/users/${id}`,
    { headers: this.getAuthHeaders() }
  );
}



}
