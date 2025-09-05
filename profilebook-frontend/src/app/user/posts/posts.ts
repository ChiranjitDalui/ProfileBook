import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-posts',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './posts.html',
  styleUrls: ['./posts.css']
})
export class PostsComponent implements OnInit {
  posts: any[] = [];

  normalizePath(path: string | null | undefined): string {
  if (!path) return '';
  // convert backslashes to forward slashes
  return path.replace(/\\/g, '/');
}


  // create post
  content = '';
  image: File | null = null;

  // search users
  userSearch = '';
  userResults: any[] = [];
  isSearching = false;

  // reporting state
  reportingBusyId: number | null = null;

// --- profile state ---
profile: any = null;
profileLoading = false;

// modal state
showProfileModal = false;
profileForm = { fullName: '', email: '', phone: '', bio: '' };
profileImageFile: File | null = null;
savingProfile = false;
uploadingProfileImage = false;


  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadPosts();
    this.loadProfile();
  }

  logout() {
    localStorage.clear();
    window.location.href = '/login';
  }

  // -------- Posts + Comments --------
 loadPosts() {
  this.api.getApprovedPosts().subscribe(
    (res: any[]) => {
      this.posts = (res || []).map(p => ({
        ...p,
        newComment: '',
        comments: [],
        likeCount:
          (typeof p.likes === 'number')
            ? p.likes
            : Array.isArray(p.likes) ? p.likes.length
            : (p.likeCount ?? 0)
      }));
      this.posts.forEach(p => this.loadCommentsForPost(p));
    },
    (err) => {
      console.error('Failed to load approved posts', err);
    }
  );
}

private loadCommentsForPost(post: any) {
  this.api.getComments(post.id).subscribe(
    (comments: any[]) => {
      post.comments = comments || [];
    },
    (err) => console.error('Failed to load comments for post', post.id, err)
  );
}


  likePost(id: number) {
    const post = this.posts.find(p => p.id === id); // find post in local array
    if (post) post.likeCount = (post.likeCount || 0) + 1;

    this.api.likePost(id).subscribe({
      next: () => { /* optimistic update already done */ },
      error: (err) => {
        console.error('Like failed', err);
        if (post) post.likeCount = Math.max((post.likeCount || 1) - 1, 0);
      }
    });
  }

  commentPost(post: any) {
    const text = (post.newComment || '').trim();
    if (!text) return;

    this.api.commentPost(post.id, text).subscribe({
      next: (res: any) => {
        const newItem =
          res?.comment ?? { text, userName: 'You', createdAt: new Date().toISOString() };
        post.comments = post.comments || [];
        post.comments.push(newItem);
        post.newComment = '';
      },
      error: (err) => console.error('Comment failed', err)
    });
  }

  // -------- Create Post --------
  onFileSelected(event: any) {
    this.image = event.target.files[0];
  }

  createPost() {
    const formData = new FormData();
    formData.append('content', this.content);
    if (this.image) formData.append('image', this.image);

    this.api.createPost(formData).subscribe({
      next: () => {
        this.content = '';
        this.image = null;
        this.loadPosts();
      },
      error: (err) => console.error('Create post failed', err)
    });
  }

  // -------- Search Users --------
  searchUsers() {
    const q = (this.userSearch || '').trim();
    if (!q) {
      this.userResults = [];
      return;
    }

    this.isSearching = true;
    this.api.searchUsers(q).subscribe({
      next: (res: any[]) => {
        this.userResults = res || [];
        this.isSearching = false;
      },
      error: (err) => {
        console.error('User search failed', err);
        this.isSearching = false;
      }
    });
  }

  // -------- Report User from Search --------
  toggleReport(u: any) {
    u._reportOpen = !u._reportOpen;
    if (u._reportOpen && !u._reason) u._reason = '';
  }

  submitReport(u: any) {
    const reason = (u._reason || '').trim();
    if (!reason) {
      alert('Please enter a reason before submitting.');
      return;
    }

    const reportedUserId = u.userId || u.UserId || u.id;
    if (!reportedUserId) {
      alert('Cannot determine user id.');
      return;
    }

    this.reportingBusyId = reportedUserId;

    this.api.reportUser(reportedUserId, reason).subscribe({
      next: (res: any) => {
        alert(res?.message || 'Reported successfully.');
        u._reportOpen = false;
        u._reason = '';
        this.reportingBusyId = null;
      },
      error: (err) => {
        console.error('Report failed', err);
        alert(err?.error?.message || 'Could not submit report. Please try again.');
        this.reportingBusyId = null;
      }
    });
  }

  // -------- Report User from Post --------
  toggleReportOnPost(post: any) {
    post._reportOpen = !post._reportOpen;
    if (post._reportOpen && !post._reason) post._reason = '';
  }

  submitReportForPost(post: any) {
    const reason = (post._reason || '').trim();
    if (!reason) {
      alert('Please enter a reason before submitting.');
      return;
    }

    const reportedUserId =
      post.userId || post.authorId || post.user?.id || post.author?.id;
    if (!reportedUserId) {
      alert('Cannot determine the author of this post.');
      return;
    }

    this.api.reportUser(reportedUserId, reason).subscribe({
      next: (res: any) => {
        alert(res?.message || 'Reported successfully.');
        post._reportOpen = false;
        post._reason = '';
      },
      error: (err) => {
        console.error('Report failed', err);
        alert(err?.error?.message || 'Could not submit report. Please try again.');
      }
    });
  }


  // -------- Profile --------
 loadProfile() {
  this.profileLoading = true;
  this.api.getMyProfile().subscribe(
    (res: any) => {
      this.profile = res;
      // prefill form from existing profile
      this.profileForm = {
        fullName: res.fullName || '',
        email: res.email || '',
        phone: res.phone || '',
        bio: res.bio || ''
      };
      this.profileLoading = false;
    },
    (err) => {
      // 404 => no profile yet; keep profile = null and empty form
      this.profile = null;
      this.profileForm = { fullName: '', email: '', phone: '', bio: '' };
      this.profileLoading = false;
    }
  );
}

// Open/close modal
openProfileModal() {
  // ensure we have freshest profile
  this.loadProfile();
  this.profileImageFile = null;
  this.showProfileModal = true;
}
closeProfileModal() {
  this.showProfileModal = false;
  this.profileImageFile = null;
}

// Save (create or update)
saveProfile() {
  if (!(this.profileForm.fullName || '').trim()) {
    alert('Full name is required.');
    return;
  }
  this.savingProfile = true;

  const op$ = this.profile
    ? this.api.updateMyProfile(this.profileForm)
    : this.api.createProfile(this.profileForm);

  op$.subscribe(
    (res: any) => {
      this.savingProfile = false;
      this.profile = res; // server returns profile object
      alert('Profile saved.');
      // If an image file was selected, upload it next
      if (this.profileImageFile) {
        this.uploadProfilePic(true); // true = stay in modal until image done
      } else {
        this.closeProfileModal();
        this.loadProfile();
      }
    },
    (err) => {
      console.error('Save profile failed', err);
      this.savingProfile = false;
      alert('Could not save profile.');
    }
  );
}

onProfileImageSelected(ev: any) {
  this.profileImageFile = ev?.target?.files?.[0] || null;
}

uploadProfilePic(stayOpen = false) {
  if (!this.profileImageFile) {
    alert('Please choose an image.');
    return;
  }
  this.uploadingProfileImage = true;
  this.api.uploadProfileImage(this.profileImageFile).subscribe(
    (res: any) => {
      this.uploadingProfileImage = false;
      alert(res?.message || 'Profile picture uploaded!');
      this.profileImageFile = null;
      this.loadProfile();
      if (!stayOpen) this.closeProfileModal();
    },
    (err) => {
      console.error('Upload profile image failed', err);
      this.uploadingProfileImage = false;
      alert('Could not upload image.');
    }
  );
}
}
