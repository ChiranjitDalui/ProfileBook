import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PostsApprove } from './posts-approve';

describe('PostsApprove', () => {
  let component: PostsApprove;
  let fixture: ComponentFixture<PostsApprove>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostsApprove]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PostsApprove);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
