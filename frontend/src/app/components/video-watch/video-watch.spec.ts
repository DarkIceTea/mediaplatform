import { ComponentFixture, TestBed } from '@angular/core/testing';
import { VideoWatch } from './video-watch';

describe('VideoWatch', () => {
  let component: VideoWatch;
  let fixture: ComponentFixture<VideoWatch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoWatch],
    }).compileComponents();

    fixture = TestBed.createComponent(VideoWatch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
